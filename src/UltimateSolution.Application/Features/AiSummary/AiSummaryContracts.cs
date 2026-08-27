using Mediator;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Application.Features.AiSummary;

public sealed record TranscriptionSegmentDto(int SequenceNumber, string Text, string? SpeakerLabel, TimeSpan StartOffset, TimeSpan EndOffset);
public sealed record TranscriptionJobDto(Guid Id, Guid MeetingId, Guid RecordingId, TranscriptionJobStatus Status, string? ExternalJobReference, DateTimeOffset RequestedAtUtc, DateTimeOffset? CompletedAtUtc, string? FailureCode, IReadOnlyCollection<TranscriptionSegmentDto> Segments);
public sealed record ProposedActionItemDto(string Title, string? Description, Guid? AssigneeUserId, DateTimeOffset? DueAtUtc);
public sealed record MeetingSummaryDto(Guid Id, Guid MeetingId, Guid TranscriptionJobId, string Content, IReadOnlyCollection<string> Decisions, IReadOnlyCollection<ProposedActionItemDto> ProposedActionItems, MeetingSummaryStatus Status, DateTimeOffset GeneratedAtUtc, DateTimeOffset? ApprovedAtUtc, Guid? ApprovedByUserId);
public sealed record ActionItemDto(Guid Id, Guid MeetingId, Guid MeetingSummaryId, string Title, string? Description, Guid? AssigneeUserId, DateTimeOffset? DueAtUtc, ActionItemStatus Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record RequestTranscriptionCommand(Guid RequestingUserId, bool IsManager, Guid RecordingId) : IRequest<TranscriptionJobDto>;
public sealed record GetMeetingTranscriptionQuery(Guid RequestingUserId, Guid MeetingId) : IRequest<TranscriptionJobDto>;
public sealed record GenerateMeetingSummaryCommand(Guid RequestingUserId, bool IsManager, Guid MeetingId) : IRequest<MeetingSummaryDto>;
public sealed record GetMeetingSummaryQuery(Guid RequestingUserId, Guid MeetingId) : IRequest<MeetingSummaryDto>;
public sealed record ApproveMeetingSummaryCommand(Guid RequestingUserId, Guid MeetingId) : IRequest<MeetingSummaryDto>;
public sealed record GetActionItemsQuery(Guid RequestingUserId) : IRequest<IReadOnlyCollection<ActionItemDto>>;
public sealed record UpdateActionItemCommand(Guid RequestingUserId, bool IsManager, Guid ActionItemId, string Title, string? Description, Guid? AssigneeUserId, DateTimeOffset? DueAtUtc, ActionItemStatus Status) : IRequest<ActionItemDto>;
