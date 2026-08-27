namespace UltimateSolution.API.Contracts.Meetings;

public sealed record ScheduleMeetingRequest(string Title, string? Agenda, DateTimeOffset ScheduledStartUtc, DateTimeOffset ScheduledEndUtc, IReadOnlyCollection<Guid> ParticipantUserIds);
public sealed record UpdateMeetingRequest(string Title, string? Agenda, DateTimeOffset ScheduledStartUtc, DateTimeOffset ScheduledEndUtc);
public sealed record InviteMeetingParticipantRequest(Guid UserId);
