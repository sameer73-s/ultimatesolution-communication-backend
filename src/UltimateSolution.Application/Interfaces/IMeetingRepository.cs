using UltimateSolution.Domain.Entities.Meetings;

namespace UltimateSolution.Application.Interfaces;

public interface IMeetingRepository
{
    Task<Meeting?> GetByIdAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Meeting>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(Meeting meeting);
    void AddRecording(MeetingRecording recording);
}
