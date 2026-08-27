using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Notifications;

namespace UltimateSolution.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(ApplicationDbContext context) : INotificationRepository
{
    public Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken = default) =>
        context.Notifications.SingleOrDefaultAsync(notification => notification.Id == notificationId, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetForRecipientAsync(Guid recipientUserId, CancellationToken cancellationToken = default) =>
        await context.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(Notification notification) => context.Notifications.Add(notification);

    public void AddRange(IEnumerable<Notification> notifications) => context.Notifications.AddRange(notifications);
}
