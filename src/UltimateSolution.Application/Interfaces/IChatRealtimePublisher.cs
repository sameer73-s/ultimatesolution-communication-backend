using UltimateSolution.Application.Features.Chat;

namespace UltimateSolution.Application.Interfaces;

public interface IChatRealtimePublisher
{
    Task PublishMessageCreatedAsync(ChatMessageDto message, CancellationToken cancellationToken = default);

    Task PublishMessageUpdatedAsync(ChatMessageDto message, CancellationToken cancellationToken = default);

    Task PublishMessageDeletedAsync(ChatMessageDto message, CancellationToken cancellationToken = default);

    Task PublishMessageReadAsync(MessageReadDto messageRead, CancellationToken cancellationToken = default);

    Task PublishPresenceChangedAsync(PresenceSnapshot presence, CancellationToken cancellationToken = default);
}
