using Microsoft.AspNetCore.SignalR;
using UltimateSolution.Application.Features.Chat;
using UltimateSolution.Application.Interfaces;

namespace UltimateSolution.Infrastructure.SignalR;

public sealed class SignalRChatRealtimePublisher(IHubContext<ChatHub> hubContext) : IChatRealtimePublisher
{
    public Task PublishMessageCreatedAsync(ChatMessageDto message, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(HubGroupNames.Channel(message.ChannelId))
            .SendAsync("messageCreated", message, cancellationToken);

    public Task PublishMessageUpdatedAsync(ChatMessageDto message, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(HubGroupNames.Channel(message.ChannelId))
            .SendAsync("messageUpdated", message, cancellationToken);

    public Task PublishMessageDeletedAsync(ChatMessageDto message, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(HubGroupNames.Channel(message.ChannelId))
            .SendAsync("messageDeleted", message, cancellationToken);

    public Task PublishMessageReadAsync(MessageReadDto messageRead, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(HubGroupNames.Channel(messageRead.ChannelId))
            .SendAsync("messageRead", messageRead, cancellationToken);

    public Task PublishPresenceChangedAsync(PresenceSnapshot presence, CancellationToken cancellationToken = default) =>
        hubContext.Clients.All.SendAsync("presenceChanged", presence, cancellationToken);
}
