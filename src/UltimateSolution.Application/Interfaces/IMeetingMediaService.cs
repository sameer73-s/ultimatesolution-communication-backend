using UltimateSolution.Application.Common.Results;
using UltimateSolution.Application.Features.Meetings;

namespace UltimateSolution.Application.Interfaces;

public interface IMeetingMediaService
{
    Task<Result<MeetingMediaSession>> StartMeetingAsync(StartMeetingMediaRequest request, CancellationToken cancellationToken = default);
    Task<Result> EndMeetingAsync(EndMeetingMediaRequest request, CancellationToken cancellationToken = default);
    Task<Result<JoinMeetingResult>> JoinParticipantAsync(JoinMeetingParticipantRequest request, CancellationToken cancellationToken = default);
    Task<Result> LeaveParticipantAsync(LeaveMeetingParticipantRequest request, CancellationToken cancellationToken = default);
    Task<Result<RecordingResult>> StartRecordingAsync(StartRecordingRequest request, CancellationToken cancellationToken = default);
    Task<Result<RecordingResult>> StopRecordingAsync(StopRecordingRequest request, CancellationToken cancellationToken = default);
}
