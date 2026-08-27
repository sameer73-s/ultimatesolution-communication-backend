using UltimateSolution.Domain.Entities.Chat;

namespace UltimateSolution.Application.Interfaces;

public interface IChatMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessage>> GetForChannelAsync(
        Guid channelId,
        string? searchTerm,
        int take,
        CancellationToken cancellationToken = default);

    void Add(ChatMessage message);
}
