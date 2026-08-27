using Mediator;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Chat;
using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Application.Features.Chat;

public sealed class CreateChannelCommandHandler(
    IChatChannelRepository channelRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateChannelCommand, ChannelDto>
{
    public async ValueTask<ChannelDto> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
    {
        var memberIds = request.MemberIds
            .Append(request.RequestingUserId)
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (request.Type == ChatChannelType.Direct && memberIds.Length != 2)
        {
            throw new DomainValidationException("A direct channel must contain exactly two distinct members.");
        }

        foreach (var memberId in memberIds)
        {
            if (!await userDirectory.ExistsAsync(memberId, cancellationToken))
            {
                throw new DomainValidationException("Every channel member must be an existing user.");
            }
        }

        if (request.Type is ChatChannelType.Group or ChatChannelType.Channel && memberIds.Length < 2)
        {
            throw new DomainValidationException("A group or channel must contain at least two members.");
        }

        if (request.Type == ChatChannelType.Direct)
        {
            var otherUserId = memberIds.Single(userId => userId != request.RequestingUserId);
            var existing = await channelRepository.GetDirectChannelAsync(
                request.RequestingUserId,
                otherUserId,
                cancellationToken);
            if (existing is not null)
            {
                return MapChannel(existing);
            }
        }

        var createdAtUtc = DateTimeOffset.UtcNow;
        var channel = ChatChannel.Create(request.Type, request.Name, request.RequestingUserId, createdAtUtc);
        foreach (var userId in memberIds)
        {
            channel.Members.Add(new ChannelMember(
                channel.Id,
                userId,
                userId == request.RequestingUserId ? ChannelMemberRole.Owner : ChannelMemberRole.Member,
                createdAtUtc));
        }

        channelRepository.Add(channel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapChannel(channel);
    }

    internal static ChannelDto MapChannel(ChatChannel channel) => new(
        channel.Id,
        channel.Name,
        channel.Type,
        channel.CreatedByUserId,
        channel.CreatedAtUtc,
        channel.IsArchived,
        channel.ArchivedAtUtc,
        channel.Members
            .OrderBy(member => member.JoinedAtUtc)
            .Select(member => new ChannelMemberDto(member.UserId, member.Role, member.JoinedAtUtc))
            .ToArray());
}

public sealed class GetChannelsQueryHandler(IChatChannelRepository channelRepository)
    : IRequestHandler<GetChannelsQuery, IReadOnlyCollection<ChannelDto>>
{
    public async ValueTask<IReadOnlyCollection<ChannelDto>> Handle(
        GetChannelsQuery request,
        CancellationToken cancellationToken)
    {
        var channels = await channelRepository.GetForUserAsync(request.RequestingUserId, cancellationToken);
        return channels.Select(CreateChannelCommandHandler.MapChannel).ToArray();
    }
}

public sealed class GetChannelQueryHandler(IChatChannelRepository channelRepository)
    : IRequestHandler<GetChannelQuery, ChannelDto>
{
    public async ValueTask<ChannelDto> Handle(GetChannelQuery request, CancellationToken cancellationToken)
    {
        var channel = await ChatAuthorization.GetAuthorizedChannelAsync(channelRepository, request.RequestingUserId, request.ChannelId, cancellationToken);
        return CreateChannelCommandHandler.MapChannel(channel);
    }
}

public sealed class AddChannelMemberCommandHandler(
    IChatChannelRepository channelRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddChannelMemberCommand, ChannelDto>
{
    public async ValueTask<ChannelDto> Handle(AddChannelMemberCommand request, CancellationToken cancellationToken)
    {
        var channel = await ChatAuthorization.GetAuthorizedChannelAsync(channelRepository, request.RequestingUserId, request.ChannelId, cancellationToken);
        ChatAuthorization.EnsureOwnerOrAdministrator(channel, request.RequestingUserId, request.IsAdministrator);

        if (channel.Type == ChatChannelType.Direct)
        {
            throw new DomainValidationException("Members cannot be added to a direct channel.");
        }

        if (channel.Members.Any(member => member.UserId == request.MemberUserId))
        {
            throw new DomainValidationException("The user is already a channel member.");
        }

        if (!await userDirectory.ExistsAsync(request.MemberUserId, cancellationToken))
        {
            throw new DomainValidationException("The channel member must be an existing user.");
        }

        channel.Members.Add(new ChannelMember(
            channel.Id,
            request.MemberUserId,
            ChannelMemberRole.Member,
            DateTimeOffset.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreateChannelCommandHandler.MapChannel(channel);
    }
}

public sealed class UpdateChannelCommandHandler(
    IChatChannelRepository channelRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateChannelCommand, ChannelDto>
{
    public async ValueTask<ChannelDto> Handle(UpdateChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await ChatAuthorization.GetAuthorizedChannelAsync(channelRepository, request.RequestingUserId, request.ChannelId, cancellationToken);
        ChatAuthorization.EnsureOwnerOrAdministrator(channel, request.RequestingUserId, request.IsAdministrator);

        if (request.Name is null && !request.IsArchived.HasValue)
        {
            throw new DomainValidationException("At least one channel update value is required.");
        }

        if (request.Name is not null)
        {
            channel.Rename(request.Name);
        }

        if (request.IsArchived.HasValue)
        {
            channel.SetArchived(request.IsArchived.Value, DateTimeOffset.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreateChannelCommandHandler.MapChannel(channel);
    }
}

public sealed class RemoveChannelMemberCommandHandler(
    IChatChannelRepository channelRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveChannelMemberCommand, ChannelDto>
{
    public async ValueTask<ChannelDto> Handle(RemoveChannelMemberCommand request, CancellationToken cancellationToken)
    {
        var channel = await ChatAuthorization.GetAuthorizedChannelAsync(channelRepository, request.RequestingUserId, request.ChannelId, cancellationToken);
        ChatAuthorization.EnsureOwnerOrAdministrator(channel, request.RequestingUserId, request.IsAdministrator);

        var member = channel.Members.SingleOrDefault(candidate => candidate.UserId == request.MemberUserId)
            ?? throw new DomainNotFoundException("The channel member was not found.");
        if (member.Role == ChannelMemberRole.Owner
            && channel.Members.Count(candidate => candidate.Role == ChannelMemberRole.Owner) == 1)
        {
            throw new DomainValidationException("The final channel owner cannot be removed.");
        }

        channel.Members.Remove(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreateChannelCommandHandler.MapChannel(channel);
    }
}

public sealed class SendChatMessageCommandHandler(
    IChatChannelRepository channelRepository,
    IChatMessageRepository messageRepository,
    IUnitOfWork unitOfWork,
    IChatRealtimePublisher realtimePublisher)
    : IRequestHandler<SendChatMessageCommand, ChatMessageDto>
{
    public async ValueTask<ChatMessageDto> Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        var channel = await ChatAuthorization.GetAuthorizedChannelAsync(channelRepository, request.RequestingUserId, request.ChannelId, cancellationToken);
        if (channel.IsArchived)
        {
            throw new DomainValidationException("Messages cannot be sent to an archived channel.");
        }

        var message = ChatMessage.Create(channel.Id, request.RequestingUserId, request.Body, DateTimeOffset.UtcNow);
        messageRepository.Add(message);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = MapMessage(message);
        await realtimePublisher.PublishMessageCreatedAsync(response, cancellationToken);
        return response;
    }

    internal static ChatMessageDto MapMessage(ChatMessage message) => new(
        message.Id,
        message.ChannelId,
        message.SenderUserId,
        message.Body,
        message.CreatedAtUtc,
        message.EditedAtUtc,
        message.DeletedAtUtc);
}

public sealed class UpdateChatMessageCommandHandler(
    IChatChannelRepository channelRepository,
    IChatMessageRepository messageRepository,
    IUnitOfWork unitOfWork,
    IChatRealtimePublisher realtimePublisher)
    : IRequestHandler<UpdateChatMessageCommand, ChatMessageDto>
{
    public async ValueTask<ChatMessageDto> Handle(UpdateChatMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(request.MessageId, cancellationToken)
            ?? throw new DomainNotFoundException("The chat message was not found.");
        await ChatAuthorization.GetAuthorizedChannelAsync(channelRepository, request.RequestingUserId, message.ChannelId, cancellationToken);
        if (message.SenderUserId != request.RequestingUserId)
        {
            throw new DomainForbiddenException("Only the message sender can edit this message.");
        }

        message.Edit(request.Body, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var response = SendChatMessageCommandHandler.MapMessage(message);
        await realtimePublisher.PublishMessageUpdatedAsync(response, cancellationToken);
        return response;
    }
}

public sealed class DeleteChatMessageCommandHandler(
    IChatChannelRepository channelRepository,
    IChatMessageRepository messageRepository,
    IUnitOfWork unitOfWork,
    IChatRealtimePublisher realtimePublisher)
    : IRequestHandler<DeleteChatMessageCommand, ChatMessageDto>
{
    public async ValueTask<ChatMessageDto> Handle(DeleteChatMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(request.MessageId, cancellationToken)
            ?? throw new DomainNotFoundException("The chat message was not found.");
        var channel = await channelRepository.GetByIdAsync(message.ChannelId, cancellationToken)
            ?? throw new DomainNotFoundException("The chat channel was not found.");
        if (!request.IsAdministrator)
        {
            await ChatAuthorization.GetAuthorizedChannelAsync(channelRepository, request.RequestingUserId, channel.Id, cancellationToken);
            if (message.SenderUserId != request.RequestingUserId)
            {
                throw new DomainForbiddenException("Only the message sender can delete this message.");
            }
        }

        message.SoftDelete(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var response = SendChatMessageCommandHandler.MapMessage(message);
        await realtimePublisher.PublishMessageDeletedAsync(response, cancellationToken);
        return response;
    }
}

public sealed class GetChannelMessagesQueryHandler(
    IChatChannelRepository channelRepository,
    IChatMessageRepository messageRepository)
    : IRequestHandler<GetChannelMessagesQuery, IReadOnlyCollection<ChatMessageDto>>
{
    public async ValueTask<IReadOnlyCollection<ChatMessageDto>> Handle(
        GetChannelMessagesQuery request,
        CancellationToken cancellationToken)
    {
        await ChatAuthorization.GetAuthorizedChannelAsync(channelRepository, request.RequestingUserId, request.ChannelId, cancellationToken);
        var messages = await messageRepository.GetForChannelAsync(
            request.ChannelId,
            request.SearchTerm,
            request.Take,
            cancellationToken);
        return messages.Select(SendChatMessageCommandHandler.MapMessage).ToArray();
    }
}

public sealed class MarkMessageReadCommandHandler(
    IChatChannelRepository channelRepository,
    IChatMessageRepository messageRepository,
    IUnitOfWork unitOfWork,
    IChatRealtimePublisher realtimePublisher)
    : IRequestHandler<MarkMessageReadCommand, MessageReadDto>
{
    public async ValueTask<MessageReadDto> Handle(MarkMessageReadCommand request, CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(request.MessageId, cancellationToken)
            ?? throw new DomainNotFoundException("The chat message was not found.");
        await ChatAuthorization.GetAuthorizedChannelAsync(channelRepository, request.RequestingUserId, message.ChannelId, cancellationToken);

        var readAtUtc = DateTimeOffset.UtcNow;
        var readState = await channelRepository.GetReadStateAsync(
            message.ChannelId,
            request.RequestingUserId,
            cancellationToken);
        if (readState is null)
        {
            readState = new MessageReadState(message.ChannelId, request.RequestingUserId, message.Id, readAtUtc);
            channelRepository.AddReadState(readState);
        }
        else
        {
            readState.MarkRead(message.Id, readAtUtc);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var response = new MessageReadDto(
            readState.ChannelId,
            readState.UserId,
            readState.LastReadMessageId,
            readState.LastReadAtUtc);
        await realtimePublisher.PublishMessageReadAsync(response, cancellationToken);
        return response;
    }
}

public sealed class VerifyChannelMembershipQueryHandler(IChatChannelRepository channelRepository)
    : IRequestHandler<VerifyChannelMembershipQuery, Unit>
{
    public async ValueTask<Unit> Handle(VerifyChannelMembershipQuery request, CancellationToken cancellationToken)
    {
        await ChatAuthorization.GetAuthorizedChannelAsync(channelRepository, request.RequestingUserId, request.ChannelId, cancellationToken);
        return Unit.Value;
    }
}

internal static class ChatAuthorization
{
    public static async Task<ChatChannel> GetAuthorizedChannelAsync(
        IChatChannelRepository channelRepository,
        Guid userId,
        Guid channelId,
        CancellationToken cancellationToken)
    {
        var channel = await channelRepository.GetByIdAsync(channelId, cancellationToken)
            ?? throw new DomainNotFoundException("The chat channel was not found.");
        if (channel.Members.All(member => member.UserId != userId))
        {
            throw new DomainForbiddenException("You are not a member of this channel.");
        }

        return channel;
    }

    public static void EnsureOwnerOrAdministrator(ChatChannel channel, Guid userId, bool isAdministrator)
    {
        if (!isAdministrator && channel.CreatedByUserId != userId)
        {
            throw new DomainForbiddenException("Only the channel owner or an administrator can perform this action.");
        }
    }
}
