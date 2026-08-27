using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Domain.Entities.Chat;

public sealed class ChatMessage
{
    private ChatMessage()
    {
    }

    private ChatMessage(Guid id, Guid channelId, Guid senderUserId, string body, DateTimeOffset createdAtUtc)
    {
        Id = id;
        ChannelId = channelId;
        SenderUserId = senderUserId;
        Body = body;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ChannelId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? EditedAtUtc { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static ChatMessage Create(Guid channelId, Guid senderUserId, string body, DateTimeOffset createdAtUtc)
    {
        if (channelId == Guid.Empty || senderUserId == Guid.Empty)
        {
            throw new DomainValidationException("A channel and sender are required.");
        }

        var normalizedBody = body?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedBody))
        {
            throw new DomainValidationException("Message body is required.");
        }

        if (normalizedBody.Length > 4000)
        {
            throw new DomainValidationException("Message body cannot exceed 4000 characters.");
        }

        return new ChatMessage(Guid.NewGuid(), channelId, senderUserId, normalizedBody, createdAtUtc);
    }

    public void Edit(string body, DateTimeOffset editedAtUtc)
    {
        if (DeletedAtUtc.HasValue)
        {
            throw new DomainValidationException("A deleted message cannot be edited.");
        }

        var normalizedBody = body?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedBody))
        {
            throw new DomainValidationException("Message body is required.");
        }

        if (normalizedBody.Length > 4000)
        {
            throw new DomainValidationException("Message body cannot exceed 4000 characters.");
        }

        Body = normalizedBody;
        EditedAtUtc = editedAtUtc;
    }

    public void SoftDelete(DateTimeOffset deletedAtUtc)
    {
        if (DeletedAtUtc.HasValue)
        {
            throw new DomainValidationException("The message is already deleted.");
        }

        DeletedAtUtc = deletedAtUtc;
    }
}
