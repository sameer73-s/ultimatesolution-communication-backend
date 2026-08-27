using Mediator;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Notifications;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Application.Features.Notifications;

public sealed class GetNotificationsQueryHandler(INotificationRepository notificationRepository) : IRequestHandler<GetNotificationsQuery, IReadOnlyCollection<NotificationDto>>
{
    public async ValueTask<IReadOnlyCollection<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken) =>
        (await notificationRepository.GetForRecipientAsync(request.RequestingUserId, cancellationToken)).Select(NotificationMapper.Map).ToArray();
}

public sealed class MarkNotificationReadCommandHandler(
    INotificationRepository notificationRepository,
    INotificationRealtimePublisher notificationRealtimePublisher,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkNotificationReadCommand, NotificationDto>
{
    public async ValueTask<NotificationDto> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new DomainNotFoundException("The notification was not found.");
        if (notification.RecipientUserId != request.RequestingUserId)
        {
            throw new DomainForbiddenException("You are not allowed to mark this notification as read.");
        }

        notification.MarkRead(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var notificationDto = NotificationMapper.Map(notification);
        await notificationRealtimePublisher.PublishNotificationReadAsync(notificationDto, cancellationToken);
        return notificationDto;
    }
}

internal static class NotificationMapper
{
    public static NotificationDto Map(Notification notification) => new(
        notification.Id,
        notification.RecipientUserId,
        notification.Type,
        notification.SourceType,
        notification.SourceId,
        notification.Title,
        notification.Body,
        notification.CreatedAtUtc,
        notification.ReadAtUtc);
}
