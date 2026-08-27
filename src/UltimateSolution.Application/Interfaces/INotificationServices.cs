using UltimateSolution.Application.Common.Results;
using UltimateSolution.Application.Features.Notifications;
using UltimateSolution.Domain.Entities.Notifications;

namespace UltimateSolution.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetForRecipientAsync(Guid recipientUserId, CancellationToken cancellationToken = default);
    void Add(Notification notification);
    void AddRange(IEnumerable<Notification> notifications);
}

public interface INotificationRealtimePublisher
{
    Task PublishNotificationCreatedAsync(NotificationDto notification, CancellationToken cancellationToken = default);
    Task PublishNotificationReadAsync(NotificationDto notification, CancellationToken cancellationToken = default);
    Task PublishActionItemsCreatedAsync(Guid recipientUserId, IReadOnlyCollection<ActionItemNotificationDto> actionItems, CancellationToken cancellationToken = default);
}

public interface IOutboundNotificationService
{
    Task<Result> SendAsync(OutboundNotificationRequest request, CancellationToken cancellationToken = default);
}

public sealed record OutboundNotificationRequest(Guid RecipientUserId, string Title, string? Body, string SourceType, Guid SourceId);
