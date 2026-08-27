using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Domain.Entities.Meetings;

public sealed class MeetingParticipant
{
    private MeetingParticipant()
    {
    }

    public MeetingParticipant(Guid meetingId, Guid userId, MeetingParticipantRole role, DateTimeOffset invitedAtUtc)
    {
        MeetingId = meetingId;
        UserId = userId;
        Role = role;
        InvitedAtUtc = invitedAtUtc;
    }

    public Guid MeetingId { get; private set; }
    public Guid UserId { get; private set; }
    public MeetingParticipantRole Role { get; private set; }
    public DateTimeOffset InvitedAtUtc { get; private set; }
    public DateTimeOffset? JoinedAtUtc { get; private set; }
    public DateTimeOffset? LeftAtUtc { get; private set; }

    public void MarkJoined(DateTimeOffset joinedAtUtc)
    {
        JoinedAtUtc = joinedAtUtc;
        LeftAtUtc = null;
    }

    public void MarkLeft(DateTimeOffset leftAtUtc)
    {
        LeftAtUtc = leftAtUtc;
    }
}
