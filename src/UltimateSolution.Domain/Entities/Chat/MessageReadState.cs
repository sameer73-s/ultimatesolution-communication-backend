namespace UltimateSolution.Domain.Entities.Chat;

public sealed class MessageReadState
{
    private MessageReadState()
    {
    }

    public MessageReadState(
        Guid channelId,
        Guid userId,
        Guid lastReadMessageId,
        DateTimeOffset lastReadAtUtc)
    {
        ChannelId = channelId;
        UserId = userId;
        LastReadMessageId = lastReadMessageId;
        LastReadAtUtc = lastReadAtUtc;
    }

    public Guid ChannelId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid LastReadMessageId { get; private set; }

    public DateTimeOffset LastReadAtUtc { get; private set; }

    public void MarkRead(Guid messageId, DateTimeOffset readAtUtc)
    {
        LastReadMessageId = messageId;
        LastReadAtUtc = readAtUtc;
    }
}
