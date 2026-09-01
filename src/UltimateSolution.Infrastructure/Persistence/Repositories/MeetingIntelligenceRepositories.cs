using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Meetings;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Infrastructure.Persistence.Repositories;

public sealed class MeetingIntelligenceRepository(ApplicationDbContext context) : IMeetingIntelligenceRepository
{
    public Task<MeetingRecording?> GetRecordingByIdAsync(Guid recordingId, CancellationToken cancellationToken = default) =>
        context.MeetingRecordings.SingleOrDefaultAsync(recording => recording.Id == recordingId, cancellationToken);

    public Task<TranscriptionJob?> GetLatestTranscriptionJobAsync(Guid meetingId, CancellationToken cancellationToken = default) =>
        context.TranscriptionJobs
            .Include(job => job.Segments)
            .Where(job => job.MeetingId == meetingId)
            .OrderByDescending(job => job.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<TranscriptionJob?> GetLatestCompletedTranscriptionJobAsync(Guid meetingId, CancellationToken cancellationToken = default) =>
        context.TranscriptionJobs
            .Include(job => job.Segments)
            .Where(job => job.MeetingId == meetingId && job.Status == TranscriptionJobStatus.Completed)
            .OrderByDescending(job => job.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<MeetingSummary?> GetLatestSummaryAsync(Guid meetingId, CancellationToken cancellationToken = default) =>
        context.MeetingSummaries
            .Where(summary => summary.MeetingId == meetingId)
            .OrderByDescending(summary => summary.GeneratedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public void AddTranscriptionJob(TranscriptionJob transcriptionJob) => context.TranscriptionJobs.Add(transcriptionJob);

    public void AddMeetingSummary(MeetingSummary meetingSummary) => context.MeetingSummaries.Add(meetingSummary);
}

public sealed class ActionItemRepository(ApplicationDbContext context) : IActionItemRepository
{
    public Task<ActionItem?> GetByIdAsync(Guid actionItemId, CancellationToken cancellationToken = default) =>
        context.ActionItems.SingleOrDefaultAsync(actionItem => actionItem.Id == actionItemId, cancellationToken);

    public async Task<IReadOnlyList<ActionItem>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.ActionItems
            .AsNoTracking()
            .Where(actionItem => actionItem.AssigneeUserId == userId)
            .OrderBy(actionItem => actionItem.Status)
            .ThenBy(actionItem => actionItem.DueAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForSourceMessageAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        context.ActionItems.AnyAsync(actionItem => actionItem.SourceMessageId == messageId, cancellationToken);

    public void Add(ActionItem actionItem) => context.ActionItems.Add(actionItem);

    public void AddRange(IEnumerable<ActionItem> actionItems) => context.ActionItems.AddRange(actionItems);
}
