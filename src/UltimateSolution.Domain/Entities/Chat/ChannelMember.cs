using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Domain.Entities.Chat;

public sealed class ChannelMember
{
    private ChannelMember()
    {
    }

    public ChannelMember(Guid channelId, Guid userId, ChannelMemberRole role, DateTimeOffset joinedAtUtc)
    {
        ChannelId = channelId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = joinedAtUtc;
    }

    public Guid ChannelId { get; private set; }

    public Guid UserId { get; private set; }

    public ChannelMemberRole Role { get; private set; }

    public DateTimeOffset JoinedAtUtc { get; private set; }
}
