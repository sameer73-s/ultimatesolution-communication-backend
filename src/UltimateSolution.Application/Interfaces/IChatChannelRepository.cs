using UltimateSolution.Domain.Entities.Chat;

namespace UltimateSolution.Application.Interfaces;

public interface IChatChannelRepository
{
    Task<ChatChannel?> GetByIdAsync(Guid channelId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatChannel>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ChatChannel?> GetDirectChannelAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken = default);

    Task<ChannelMember?> GetMembershipAsync(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<MessageReadState?> GetReadStateAsync(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(ChatChannel channel);

    void AddReadState(MessageReadState readState);
}
