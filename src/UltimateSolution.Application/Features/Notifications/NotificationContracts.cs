using Mediator;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Application.Features.Notifications;

public sealed record NotificationDto(Guid Id, Guid RecipientUserId, NotificationType Type, string SourceType, Guid SourceId, string Title, string? Body, DateTimeOffset CreatedAtUtc, DateTimeOffset? ReadAtUtc);
public sealed record ActionItemNotificationDto(Guid Id, Guid? MeetingId, string Title, DateTimeOffset? DueAtUtc);

public sealed record GetNotificationsQuery(Guid RequestingUserId) : IRequest<IReadOnlyCollection<NotificationDto>>;
public sealed record MarkNotificationReadCommand(Guid RequestingUserId, Guid NotificationId) : IRequest<NotificationDto>;
