using UltimateSolution.Domain.Entities.Meetings;

namespace UltimateSolution.Application.Interfaces;

public interface IMeetingIntelligenceRepository
{
    Task<MeetingRecording?> GetRecordingByIdAsync(Guid recordingId, CancellationToken cancellationToken = default);
    Task<TranscriptionJob?> GetLatestTranscriptionJobAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task<TranscriptionJob?> GetLatestCompletedTranscriptionJobAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task<MeetingSummary?> GetLatestSummaryAsync(Guid meetingId, CancellationToken cancellationToken = default);
    void AddTranscriptionJob(TranscriptionJob transcriptionJob);
    void AddMeetingSummary(MeetingSummary meetingSummary);
}

public interface IActionItemRepository
{
    Task<ActionItem?> GetByIdAsync(Guid actionItemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActionItem>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void AddRange(IEnumerable<ActionItem> actionItems);
}
