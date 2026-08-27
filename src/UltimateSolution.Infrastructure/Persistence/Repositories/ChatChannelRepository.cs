using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Chat;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Infrastructure.Persistence.Repositories;

public sealed class ChatChannelRepository(ApplicationDbContext context) : IChatChannelRepository
{
    public Task<ChatChannel?> GetByIdAsync(Guid channelId, CancellationToken cancellationToken = default) =>
        context.ChatChannels
            .Include(channel => channel.Members)
            .SingleOrDefaultAsync(channel => channel.Id == channelId, cancellationToken);

    public async Task<IReadOnlyList<ChatChannel>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.ChatChannels
            .AsNoTracking()
            .Include(channel => channel.Members)
            .Where(channel => channel.Members.Any(member => member.UserId == userId))
            .OrderByDescending(channel => channel.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<ChatChannel?> GetDirectChannelAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken = default) =>
        context.ChatChannels
            .Include(channel => channel.Members)
            .SingleOrDefaultAsync(
                channel => channel.Type == ChatChannelType.Direct
                    && channel.Members.Count == 2
                    && channel.Members.Any(member => member.UserId == firstUserId)
                    && channel.Members.Any(member => member.UserId == secondUserId),
                cancellationToken);

    public Task<ChannelMember?> GetMembershipAsync(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        context.ChannelMembers.SingleOrDefaultAsync(
            member => member.ChannelId == channelId && member.UserId == userId,
            cancellationToken);

    public Task<MessageReadState?> GetReadStateAsync(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        context.MessageReadStates.SingleOrDefaultAsync(
            readState => readState.ChannelId == channelId && readState.UserId == userId,
            cancellationToken);

    public void Add(ChatChannel channel) => context.ChatChannels.Add(channel);

    public void AddReadState(MessageReadState readState) => context.MessageReadStates.Add(readState);
}
