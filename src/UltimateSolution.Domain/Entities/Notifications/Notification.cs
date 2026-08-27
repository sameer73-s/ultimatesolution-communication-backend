using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Domain.Entities.Notifications;

public sealed class Notification
{
    private Notification()
    {
    }

    private Notification(Guid recipientUserId, NotificationType type, string sourceType, Guid sourceId, string title, string? body, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        RecipientUserId = recipientUserId;
        Type = type;
        SourceType = sourceType;
        SourceId = sourceId;
        Title = title;
        Body = body;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Body { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    public static Notification Create(Guid recipientUserId, NotificationType type, string sourceType, Guid sourceId, string title, string? body, DateTimeOffset createdAtUtc)
    {
        if (recipientUserId == Guid.Empty || sourceId == Guid.Empty)
        {
            throw new DomainValidationException("A notification recipient and source are required.");
        }

        if (string.IsNullOrWhiteSpace(sourceType) || sourceType.Trim().Length > 100)
        {
            throw new DomainValidationException("Notification source type is required and cannot exceed 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
        {
            throw new DomainValidationException("Notification title is required and cannot exceed 200 characters.");
        }

        if (body?.Length > 2000)
        {
            throw new DomainValidationException("Notification body cannot exceed 2000 characters.");
        }

        return new Notification(recipientUserId, type, sourceType.Trim(), sourceId, title.Trim(), string.IsNullOrWhiteSpace(body) ? null : body.Trim(), createdAtUtc);
    }

    public void MarkRead(DateTimeOffset readAtUtc)
    {
        if (ReadAtUtc.HasValue)
        {
            throw new DomainValidationException("The notification has already been marked as read.");
        }

        ReadAtUtc = readAtUtc;
    }
}
