using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Application.Features.Meetings;

public sealed record StartMeetingMediaRequest(Guid MeetingId, Guid OrganizerUserId, DateTimeOffset ScheduledStartUtc, IReadOnlyCollection<Guid> ParticipantUserIds);
public sealed record EndMeetingMediaRequest(Guid MeetingId, Guid RequestedByUserId, string MediaSessionReference);
public sealed record JoinMeetingParticipantRequest(Guid MeetingId, Guid UserId, MeetingParticipantRole ParticipantRole, string MediaSessionReference);
public sealed record LeaveMeetingParticipantRequest(Guid MeetingId, Guid UserId, string MediaSessionReference);
public sealed record StartRecordingRequest(Guid MeetingId, Guid RequestedByUserId, string MediaSessionReference);
public sealed record StopRecordingRequest(Guid MeetingId, Guid RequestedByUserId, string MediaSessionReference, string MediaRecordingReference);
public sealed record MeetingMediaSession(string MediaSessionReference, DateTimeOffset ExpiresAtUtc, string Status);
public sealed record JoinMeetingResult(string MediaSessionReference, string MediaJoinUrl, DateTimeOffset ExpiresAtUtc);
public sealed record RecordingResult(string MediaRecordingReference, RecordingStatus Status, DateTimeOffset? AvailableAtUtc);
