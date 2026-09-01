using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Domain.Entities.Chat;

public sealed class ChatChannel
{
    private ChatChannel()
    {
    }

    private ChatChannel(Guid id, ChatChannelType type, string name, Guid createdByUserId, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Type = type;
        Name = name;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ChatChannelType Type { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public ICollection<ChannelMember> Members { get; } = new List<ChannelMember>();

    public ICollection<ChatMessage> Messages { get; } = new List<ChatMessage>();

    public static ChatChannel Create(
        ChatChannelType type,
        string? name,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new DomainValidationException("A channel creator is required.");
        }

        var normalizedName = type == ChatChannelType.Direct
            ? "Direct message"
            : name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new DomainValidationException("A name is required for a group or channel.");
        }

        if (normalizedName.Length > 120)
        {
            throw new DomainValidationException("Channel name cannot exceed 120 characters.");
        }

        return new ChatChannel(Guid.NewGuid(), type, normalizedName, createdByUserId, createdAtUtc);
    }

    public void Rename(string name)
    {
        if (Type == ChatChannelType.Direct)
        {
            throw new DomainValidationException("A direct channel cannot be renamed.");
        }

        var normalizedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new DomainValidationException("Channel name is required.");
        }

        if (normalizedName.Length > 120)
        {
            throw new DomainValidationException("Channel name cannot exceed 120 characters.");
        }

        Name = normalizedName;
    }

    public void SetArchived(bool isArchived, DateTimeOffset occurredAtUtc)
    {
        IsArchived = isArchived;
        ArchivedAtUtc = isArchived ? occurredAtUtc : null;
    }
}
