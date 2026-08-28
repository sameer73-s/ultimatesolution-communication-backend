using System.Text.Json;
using Mediator;
using UltimateSolution.Application.Common.Results;
using UltimateSolution.Application.Features.Notifications;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Meetings;
using UltimateSolution.Domain.Entities.Notifications;
using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Application.Features.AiSummary;

public sealed class RequestTranscriptionCommandHandler(
    IMeetingRepository meetingRepository,
    IMeetingIntelligenceRepository intelligenceRepository,
    ITranscriptionService transcriptionService,
    IUnitOfWork unitOfWork) : IRequestHandler<RequestTranscriptionCommand, TranscriptionJobDto>
{
    public async ValueTask<TranscriptionJobDto> Handle(RequestTranscriptionCommand request, CancellationToken cancellationToken)
    {
        var recording = await intelligenceRepository.GetRecordingByIdAsync(request.RecordingId, cancellationToken)
            ?? throw new DomainNotFoundException("The meeting recording was not found.");
        var meeting = await AiSummaryAuthorization.GetMeetingAsync(meetingRepository, recording.MeetingId, cancellationToken);
        AiSummaryAuthorization.EnsureOrganizerOrManager(meeting, request.RequestingUserId, request.IsManager);
        if (recording.Status == RecordingStatus.Recording)
        {
            throw new DomainValidationException("Only a stopped meeting recording can be transcribed.");
        }

        var transcriptionJob = TranscriptionJob.Queue(meeting.Id, recording.Id, recording.MediaRecordingReference, DateTimeOffset.UtcNow);
        intelligenceRepository.AddTranscriptionJob(transcriptionJob);
        var submission = await transcriptionService.SubmitAsync(
            new TranscriptionSubmissionRequest(meeting.Id, recording.Id, recording.MediaRecordingReference),
            cancellationToken);
        if (!submission.IsSuccess || submission.Value is null)
        {
            transcriptionJob.Fail(submission.ErrorCode ?? "transcription_submission_failed");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new DomainValidationException("The transcription request could not be submitted.");
        }

        if (submission.Value.IsCompleted)
        {
            transcriptionJob.Complete(
                submission.Value.Segments.Select(segment => new TranscriptionSegment(
                    transcriptionJob.Id,
                    segment.SequenceNumber,
                    segment.Text,
                    segment.SpeakerLabel,
                    segment.StartOffset,
                    segment.EndOffset)),
                DateTimeOffset.UtcNow);
        }
        else
        {
            transcriptionJob.MarkProcessing(submission.Value.ExternalJobReference);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return AiSummaryMapper.Map(transcriptionJob);
    }
}

public sealed class GetMeetingTranscriptionQueryHandler(
    IMeetingRepository meetingRepository,
    IMeetingIntelligenceRepository intelligenceRepository) : IRequestHandler<GetMeetingTranscriptionQuery, TranscriptionJobDto>
{
    public async ValueTask<TranscriptionJobDto> Handle(GetMeetingTranscriptionQuery request, CancellationToken cancellationToken)
    {
        await AiSummaryAuthorization.GetParticipantMeetingAsync(meetingRepository, request.RequestingUserId, request.MeetingId, cancellationToken);
        var transcriptionJob = await intelligenceRepository.GetLatestTranscriptionJobAsync(request.MeetingId, cancellationToken)
            ?? throw new DomainNotFoundException("No transcription job was found for the meeting.");
        return AiSummaryMapper.Map(transcriptionJob);
    }
}

public sealed class GenerateMeetingSummaryCommandHandler(
    IMeetingRepository meetingRepository,
    IMeetingIntelligenceRepository intelligenceRepository,
    INotificationRepository notificationRepository,
    INotificationRealtimePublisher notificationRealtimePublisher,
    ISummaryService summaryService,
    IUnitOfWork unitOfWork) : IRequestHandler<GenerateMeetingSummaryCommand, MeetingSummaryDto>
{
    public async ValueTask<MeetingSummaryDto> Handle(GenerateMeetingSummaryCommand request, CancellationToken cancellationToken)
    {
        var meeting = await AiSummaryAuthorization.GetMeetingAsync(meetingRepository, request.MeetingId, cancellationToken);
        AiSummaryAuthorization.EnsureOrganizerOrManager(meeting, request.RequestingUserId, request.IsManager);
        var transcriptionJob = await intelligenceRepository.GetLatestCompletedTranscriptionJobAsync(meeting.Id, cancellationToken)
            ?? throw new DomainValidationException("A completed transcription is required before generating a meeting summary.");
        var existingSummary = await intelligenceRepository.GetLatestSummaryAsync(meeting.Id, cancellationToken);
        if (existingSummary?.TranscriptionJobId == transcriptionJob.Id)
        {
            return AiSummaryMapper.Map(existingSummary);
        }

        var transcript = string.Join(Environment.NewLine, transcriptionJob.Segments.OrderBy(segment => segment.SequenceNumber).Select(segment => segment.Text));
        var generatedSummary = await summaryService.GenerateAsync(
            new GenerateMeetingSummaryRequest(
                meeting.Id,
                transcriptionJob.Id,
                transcript,
                meeting.Participants.Select(participant => new MeetingSummaryParticipant(participant.UserId)).ToArray()),
            cancellationToken);
        if (!generatedSummary.IsSuccess || generatedSummary.Value is null)
        {
            throw new DomainValidationException("The meeting summary could not be generated.");
        }

        var summary = MeetingSummary.CreateDraft(
            meeting.Id,
            transcriptionJob.Id,
            generatedSummary.Value.Content,
            JsonSerializer.Serialize(generatedSummary.Value.Decisions),
            JsonSerializer.Serialize(generatedSummary.Value.ProposedActionItems),
            generatedSummary.Value.ExternalSummaryReference,
            DateTimeOffset.UtcNow);
        intelligenceRepository.AddMeetingSummary(summary);
        var notifications = meeting.Participants
            .Select(participant => Notification.Create(
                participant.UserId,
                NotificationType.MeetingSummaryReady,
                nameof(MeetingSummary),
                summary.Id,
                "Meeting summary ready for review",
                $"A draft summary is available for {meeting.Title}.",
                DateTimeOffset.UtcNow))
            .ToArray();
        notificationRepository.AddRange(notifications);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            await notificationRealtimePublisher.PublishNotificationCreatedAsync(NotificationMapper.Map(notification), cancellationToken);
        }

        return AiSummaryMapper.Map(summary);
    }
}

public sealed class GetMeetingSummaryQueryHandler(
    IMeetingRepository meetingRepository,
    IMeetingIntelligenceRepository intelligenceRepository) : IRequestHandler<GetMeetingSummaryQuery, MeetingSummaryDto>
{
    public async ValueTask<MeetingSummaryDto> Handle(GetMeetingSummaryQuery request, CancellationToken cancellationToken)
    {
        await AiSummaryAuthorization.GetParticipantMeetingAsync(meetingRepository, request.RequestingUserId, request.MeetingId, cancellationToken);
        var summary = await intelligenceRepository.GetLatestSummaryAsync(request.MeetingId, cancellationToken)
            ?? throw new DomainNotFoundException("No meeting summary was found for the meeting.");
        return AiSummaryMapper.Map(summary);
    }
}

public sealed class ApproveMeetingSummaryCommandHandler(
    IMeetingRepository meetingRepository,
    IMeetingIntelligenceRepository intelligenceRepository,
    IActionItemRepository actionItemRepository,
    INotificationRepository notificationRepository,
    INotificationRealtimePublisher notificationRealtimePublisher,
    IMeetingSummaryApprovalPolicy approvalPolicy,
    IUnitOfWork unitOfWork) : IRequestHandler<ApproveMeetingSummaryCommand, MeetingSummaryDto>
{
    public async ValueTask<MeetingSummaryDto> Handle(ApproveMeetingSummaryCommand request, CancellationToken cancellationToken)
    {
        var meeting = await AiSummaryAuthorization.GetMeetingAsync(meetingRepository, request.MeetingId, cancellationToken);
        var summary = await intelligenceRepository.GetLatestSummaryAsync(meeting.Id, cancellationToken)
            ?? throw new DomainNotFoundException("No meeting summary was found for the meeting.");
        var authorization = await approvalPolicy.AuthorizeAsync(
            new MeetingSummaryApprovalAuthorizationRequest(meeting.Id, summary.Id, request.RequestingUserId, meeting.OrganizerUserId),
            cancellationToken);
        if (!authorization.IsSuccess)
        {
            throw new DomainForbiddenException("You are not authorized to approve this meeting summary.");
        }

        var proposedActionItems = AiSummaryMapper.DeserializeProposedActionItems(summary.ProposedActionItemsJson);
        foreach (var proposedActionItem in proposedActionItems.Where(actionItem => actionItem.AssigneeUserId.HasValue))
        {
            if (meeting.Participants.All(participant => participant.UserId != proposedActionItem.AssigneeUserId))
            {
                throw new DomainValidationException("A proposed action item assignee must be a participant of the meeting.");
            }
        }

        summary.Approve(request.RequestingUserId, DateTimeOffset.UtcNow);
        var actionItems = proposedActionItems
            .Select(proposedActionItem => ActionItem.Create(
                meeting.Id,
                summary.Id,
                proposedActionItem.Title,
                proposedActionItem.Description,
                proposedActionItem.AssigneeUserId,
                proposedActionItem.DueAtUtc,
                DateTimeOffset.UtcNow))
            .ToArray();
        actionItemRepository.AddRange(actionItems);
        var notifications = actionItems
            .Where(actionItem => actionItem.AssigneeUserId.HasValue)
            .Select(actionItem => Notification.Create(
                actionItem.AssigneeUserId!.Value,
                NotificationType.ActionItemAssigned,
                nameof(ActionItem),
                actionItem.Id,
                "New action item assigned",
                actionItem.Title,
                DateTimeOffset.UtcNow))
            .ToArray();
        notificationRepository.AddRange(notifications);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            await notificationRealtimePublisher.PublishNotificationCreatedAsync(NotificationMapper.Map(notification), cancellationToken);
        }

        foreach (var actionItemsForRecipient in actionItems.Where(actionItem => actionItem.AssigneeUserId.HasValue).GroupBy(actionItem => actionItem.AssigneeUserId!.Value))
        {
            var actionItemNotifications = actionItemsForRecipient
                .Select(actionItem => new ActionItemNotificationDto(actionItem.Id, actionItem.MeetingId, actionItem.Title, actionItem.DueAtUtc))
                .ToArray();
            await notificationRealtimePublisher.PublishActionItemsCreatedAsync(actionItemsForRecipient.Key, actionItemNotifications, cancellationToken);
        }

        return AiSummaryMapper.Map(summary);
    }
}

public sealed class GetActionItemsQueryHandler(IActionItemRepository actionItemRepository) : IRequestHandler<GetActionItemsQuery, IReadOnlyCollection<ActionItemDto>>
{
    public async ValueTask<IReadOnlyCollection<ActionItemDto>> Handle(GetActionItemsQuery request, CancellationToken cancellationToken) =>
        (await actionItemRepository.GetForUserAsync(request.RequestingUserId, cancellationToken)).Select(AiSummaryMapper.Map).ToArray();
}

public sealed class UpdateActionItemCommandHandler(
    IActionItemRepository actionItemRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateActionItemCommand, ActionItemDto>
{
    public async ValueTask<ActionItemDto> Handle(UpdateActionItemCommand request, CancellationToken cancellationToken)
    {
        var actionItem = await actionItemRepository.GetByIdAsync(request.ActionItemId, cancellationToken)
            ?? throw new DomainNotFoundException("The action item was not found.");
        if (!request.IsManager && actionItem.AssigneeUserId != request.RequestingUserId)
        {
            throw new DomainForbiddenException("Only the assignee or a manager can update this action item.");
        }

        if (request.AssigneeUserId.HasValue && !await userDirectory.ExistsAsync(request.AssigneeUserId.Value, cancellationToken))
        {
            throw new DomainValidationException("The action item assignee must be an existing user.");
        }

        actionItem.Update(request.Title, request.Description, request.AssigneeUserId, request.DueAtUtc, request.Status, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return AiSummaryMapper.Map(actionItem);
    }
}

internal static class AiSummaryAuthorization
{
    public static async Task<Meeting> GetMeetingAsync(IMeetingRepository repository, Guid meetingId, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(meetingId, cancellationToken) ?? throw new DomainNotFoundException("The meeting was not found.");

    public static async Task<Meeting> GetParticipantMeetingAsync(IMeetingRepository repository, Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await GetMeetingAsync(repository, meetingId, cancellationToken);
        if (meeting.Participants.All(participant => participant.UserId != userId))
        {
            throw new DomainForbiddenException("You are not a participant of this meeting.");
        }

        return meeting;
    }

    public static void EnsureOrganizerOrManager(Meeting meeting, Guid userId, bool isManager)
    {
        if (!isManager && meeting.OrganizerUserId != userId)
        {
            throw new DomainForbiddenException("Only the meeting organizer or a manager can perform this action.");
        }
    }
}

internal static class AiSummaryMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static TranscriptionJobDto Map(TranscriptionJob transcriptionJob) => new(
        transcriptionJob.Id,
        transcriptionJob.MeetingId,
        transcriptionJob.RecordingId,
        transcriptionJob.Status,
        transcriptionJob.ExternalJobReference,
        transcriptionJob.RequestedAtUtc,
        transcriptionJob.CompletedAtUtc,
        transcriptionJob.FailureCode,
        transcriptionJob.Segments
            .OrderBy(segment => segment.SequenceNumber)
            .Select(segment => new TranscriptionSegmentDto(segment.SequenceNumber, segment.Text, segment.SpeakerLabel, segment.StartOffset, segment.EndOffset))
            .ToArray());

    public static MeetingSummaryDto Map(MeetingSummary summary) => new(
        summary.Id,
        summary.MeetingId,
        summary.TranscriptionJobId,
        summary.Content,
        Deserialize<string>(summary.DecisionsJson),
        DeserializeProposedActionItems(summary.ProposedActionItemsJson),
        summary.Status,
        summary.GeneratedAtUtc,
        summary.ApprovedAtUtc,
        summary.ApprovedByUserId);

    public static ActionItemDto Map(ActionItem actionItem) => new(
        actionItem.Id,
        actionItem.MeetingId,
        actionItem.MeetingSummaryId,
        actionItem.Title,
        actionItem.Description,
        actionItem.AssigneeUserId,
        actionItem.DueAtUtc,
        actionItem.Status,
        actionItem.CreatedAtUtc,
        actionItem.UpdatedAtUtc);

    public static IReadOnlyCollection<ProposedActionItemDto> DeserializeProposedActionItems(string json) => Deserialize<ProposedActionItemDto>(json);

    private static T[] Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T[]>(json, SerializerOptions) ?? Array.Empty<T>();
        }
        catch (JsonException)
        {
            throw new DomainValidationException("The generated meeting intelligence payload is invalid.");
        }
    }
}
