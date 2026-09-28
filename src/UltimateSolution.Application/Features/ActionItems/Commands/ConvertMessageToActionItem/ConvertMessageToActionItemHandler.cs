using Mediator;
using UltimateSolution.Application.Common.Results;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Meetings;
using UltimateSolution.Domain.Entities.Projects;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Application.Features.ActionItems.Commands.ConvertMessageToActionItem;

public sealed class ConvertMessageToActionItemHandler(
    IChatMessageRepository messageRepository,
    IChatChannelRepository channelRepository,
    IActionItemRepository actionItemRepository,
    IActionItemAuthorizationService authorizationService,
    IOutboundNotificationService notificationService,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConvertMessageToActionItemCommand, Result>
{
    public async ValueTask<Result> Handle(ConvertMessageToActionItemCommand request, CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (message == null || message.DeletedAtUtc != null)
        {
            return Result.Failure("Message.NotFound");
        }

        var isAuthorized = await authorizationService.CanConvertMessageToActionItemAsync(request.UserId, message, cancellationToken);
        if (!isAuthorized)
        {
            return Result.Failure("ActionItem.Unauthorized");
        }

        var alreadyExists = await actionItemRepository.ExistsForSourceMessageAsync(request.MessageId, cancellationToken);
        if (alreadyExists)
        {
            return Result.Failure("ActionItem.AlreadyConverted");
        }

        var channel = await channelRepository.GetByIdAsync(message.ChannelId, cancellationToken);
        if (channel == null)
        {
            return Result.Failure("Channel.NotFound");
        }

        var actionItem = ActionItem.CreateFromMessage(
            message.Id,
            channel.ProjectId,
            request.Title,
            null, // Description
            request.AssigneeUserId,
            null, // ReviewerUserId
            request.Priority,
            request.DueAtUtc,
            DateTimeOffset.UtcNow
        );

        actionItemRepository.Add(actionItem);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateException" && (ex.InnerException?.Message.Contains("23505") == true || ex.InnerException?.Message.Contains("IX_ActionItems_SourceMessageId") == true))
        {
            // Second layer of protection against race conditions
            return Result.Failure("ActionItem.AlreadyConverted");
        }

        // Dispatch notifications
        var notificationTasks = new List<Task>();
        
        notificationTasks.Add(notificationService.SendAsync(new OutboundNotificationRequest(
            request.AssigneeUserId,
            "New Action Item Assigned",
            $"You have been assigned to: {actionItem.Title}",
            "ActionItem",
            actionItem.Id
        ), cancellationToken));

        if (actionItem.ReviewerUserId.HasValue)
        {
            notificationTasks.Add(notificationService.SendAsync(new OutboundNotificationRequest(
                actionItem.ReviewerUserId.Value,
                "New Action Item Needs Review",
                $"You are the reviewer for: {actionItem.Title}",
                "ActionItem",
                actionItem.Id
            ), cancellationToken));
        }

        if (channel.ProjectId.HasValue)
        {
            var projectMembers = await projectRepository.GetMembersAsync(channel.ProjectId.Value, cancellationToken);
            var projectManager = projectMembers.FirstOrDefault(m => m.Role == ProjectMemberRole.Manager);
            if (projectManager != null)
            {
                notificationTasks.Add(notificationService.SendAsync(new OutboundNotificationRequest(
                    projectManager.UserId,
                    "New Action Item Created in Project",
                    $"A new action item was created in project: {actionItem.Title}",
                    "ActionItem",
                    actionItem.Id
                ), cancellationToken));
            }
        }

        await Task.WhenAll(notificationTasks);

        return Result.Success();
    }
}
