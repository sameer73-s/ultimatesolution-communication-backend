using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Chat;

namespace UltimateSolution.Infrastructure.Persistence.Repositories;

public sealed class ChatMessageRepository(ApplicationDbContext context) : IChatMessageRepository
{
    public Task<ChatMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        context.ChatMessages
            .SingleOrDefaultAsync(message => message.Id == messageId, cancellationToken);

    public async Task<IReadOnlyList<ChatMessage>> GetForChannelAsync(
        Guid channelId,
        string? searchTerm,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = context.ChatMessages
            .AsNoTracking()
            .Where(message => message.ChannelId == channelId && message.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedTerm = searchTerm.Trim();
            query = query.Where(message => message.Body.Contains(normalizedTerm));
        }

        return await query
            .OrderByDescending(message => message.CreatedAtUtc)
            .Take(take)
            .OrderBy(message => message.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public void Add(ChatMessage message) => context.ChatMessages.Add(message);
}
