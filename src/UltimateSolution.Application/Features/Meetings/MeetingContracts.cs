using Mediator;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Application.Features.Meetings;

public sealed record MeetingParticipantDto(Guid UserId, MeetingParticipantRole Role, DateTimeOffset InvitedAtUtc, DateTimeOffset? JoinedAtUtc, DateTimeOffset? LeftAtUtc);
public sealed record MeetingRecordingDto(Guid Id, string MediaRecordingReference, RecordingStatus Status, DateTimeOffset StartedAtUtc, DateTimeOffset? StoppedAtUtc, DateTimeOffset? AvailableAtUtc);
public sealed record MeetingDto(Guid Id, string Title, string? Agenda, Guid OrganizerUserId, DateTimeOffset ScheduledStartUtc, DateTimeOffset ScheduledEndUtc, MeetingStatus Status, string? MediaSessionReference, DateTimeOffset? StartedAtUtc, DateTimeOffset? EndedAtUtc, IReadOnlyCollection<MeetingParticipantDto> Participants, IReadOnlyCollection<MeetingRecordingDto> Recordings);

public sealed record ScheduleMeetingCommand(Guid RequestingUserId, string Title, string? Agenda, DateTimeOffset ScheduledStartUtc, DateTimeOffset ScheduledEndUtc, IReadOnlyCollection<Guid> ParticipantUserIds) : IRequest<MeetingDto>;
public sealed record GetMeetingsQuery(Guid RequestingUserId) : IRequest<IReadOnlyCollection<MeetingDto>>;
public sealed record GetMeetingQuery(Guid RequestingUserId, Guid MeetingId) : IRequest<MeetingDto>;
public sealed record UpdateMeetingCommand(Guid RequestingUserId, bool IsManager, Guid MeetingId, string Title, string? Agenda, DateTimeOffset ScheduledStartUtc, DateTimeOffset ScheduledEndUtc) : IRequest<MeetingDto>;
public sealed record InviteMeetingParticipantCommand(Guid RequestingUserId, bool IsManager, Guid MeetingId, Guid ParticipantUserId) : IRequest<MeetingDto>;
public sealed record RemoveMeetingParticipantCommand(Guid RequestingUserId, bool IsManager, Guid MeetingId, Guid ParticipantUserId) : IRequest<MeetingDto>;
public sealed record StartMeetingCommand(Guid RequestingUserId, bool IsManager, Guid MeetingId) : IRequest<MeetingDto>;
public sealed record EndMeetingCommand(Guid RequestingUserId, bool IsManager, Guid MeetingId) : IRequest<MeetingDto>;
public sealed record JoinMeetingCommand(Guid RequestingUserId, Guid MeetingId) : IRequest<JoinMeetingResult>;
public sealed record LeaveMeetingCommand(Guid RequestingUserId, Guid MeetingId) : IRequest<Unit>;
public sealed record StartMeetingRecordingCommand(Guid RequestingUserId, bool IsManager, Guid MeetingId) : IRequest<MeetingRecordingDto>;
public sealed record StopMeetingRecordingCommand(Guid RequestingUserId, bool IsManager, Guid MeetingId) : IRequest<MeetingRecordingDto>;
public sealed record GetMeetingRecordingsQuery(Guid RequestingUserId, Guid MeetingId) : IRequest<IReadOnlyCollection<MeetingRecordingDto>>;
