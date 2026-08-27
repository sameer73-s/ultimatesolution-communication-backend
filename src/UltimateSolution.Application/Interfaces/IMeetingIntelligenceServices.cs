using UltimateSolution.Application.Common.Results;
using UltimateSolution.Application.Features.AiSummary;

namespace UltimateSolution.Application.Interfaces;

public interface ITranscriptionService
{
    Task<Result<TranscriptionSubmissionResult>> SubmitAsync(TranscriptionSubmissionRequest request, CancellationToken cancellationToken);
}

public interface ISummaryService
{
    Task<Result<GeneratedMeetingSummary>> GenerateAsync(GenerateMeetingSummaryRequest request, CancellationToken cancellationToken);
}

public interface IMeetingSummaryApprovalPolicy
{
    Task<Result> AuthorizeAsync(MeetingSummaryApprovalAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record TranscriptionSubmissionRequest(Guid MeetingId, Guid RecordingId, string MediaRecordingReference);
public sealed record TranscriptionSubmissionResult(string ExternalJobReference, IReadOnlyCollection<TranscriptionSegmentDto> Segments, bool IsCompleted);
public sealed record GenerateMeetingSummaryRequest(Guid MeetingId, Guid TranscriptionJobId, string Transcript, IReadOnlyCollection<MeetingSummaryParticipant> Participants);
public sealed record MeetingSummaryParticipant(Guid UserId);
public sealed record GeneratedMeetingSummary(string Content, IReadOnlyCollection<string> Decisions, IReadOnlyCollection<ProposedActionItemDto> ProposedActionItems, string? ExternalSummaryReference);
public sealed record MeetingSummaryApprovalAuthorizationRequest(Guid MeetingId, Guid MeetingSummaryId, Guid RequestingUserId, Guid OrganizerUserId);
