using Microsoft.AspNetCore.SignalR;
using UltimateSolution.Application.Features.Notifications;
using UltimateSolution.Application.Interfaces;

namespace UltimateSolution.Infrastructure.SignalR;

public sealed class SignalRNotificationRealtimePublisher(IHubContext<NotificationsHub> hubContext) : INotificationRealtimePublisher
{
    public Task PublishNotificationCreatedAsync(NotificationDto notification, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(HubGroupNames.User(notification.RecipientUserId))
            .SendAsync("notificationCreated", notification, cancellationToken);

    public Task PublishNotificationReadAsync(NotificationDto notification, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(HubGroupNames.User(notification.RecipientUserId))
            .SendAsync("notificationRead", notification, cancellationToken);

    public Task PublishActionItemsCreatedAsync(Guid recipientUserId, IReadOnlyCollection<ActionItemNotificationDto> actionItems, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(HubGroupNames.User(recipientUserId))
            .SendAsync("actionItemsCreated", actionItems, cancellationToken);
}
