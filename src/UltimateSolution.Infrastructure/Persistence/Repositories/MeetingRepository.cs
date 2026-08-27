using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Meetings;

namespace UltimateSolution.Infrastructure.Persistence.Repositories;

public sealed class MeetingRepository(ApplicationDbContext context) : IMeetingRepository
{
    public Task<Meeting?> GetByIdAsync(Guid meetingId, CancellationToken cancellationToken = default) => context.Meetings.Include(meeting => meeting.Participants).Include(meeting => meeting.Recordings).SingleOrDefaultAsync(meeting => meeting.Id == meetingId, cancellationToken);
    public async Task<IReadOnlyList<Meeting>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) => await context.Meetings.AsNoTracking().Include(meeting => meeting.Participants).Include(meeting => meeting.Recordings).Where(meeting => meeting.Participants.Any(participant => participant.UserId == userId)).OrderBy(meeting => meeting.ScheduledStartUtc).ToListAsync(cancellationToken);
    public void Add(Meeting meeting) => context.Meetings.Add(meeting);

    public void AddRecording(MeetingRecording recording) => context.MeetingRecordings.Add(recording);
}
