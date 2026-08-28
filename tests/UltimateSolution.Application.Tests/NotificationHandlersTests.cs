using UltimateSolution.Application.Features.Notifications;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Notifications;
using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Application.Tests;

public sealed class NotificationHandlersTests
{
    [Fact]
    public async Task MarkReadHandlerPersistsThenPublishesTheRecipientNotification()
    {
        var recipientUserId = Guid.NewGuid();
        var notification = Notification.Create(recipientUserId, NotificationType.General, "Test", Guid.NewGuid(), "Test notification", null, DateTimeOffset.UtcNow);
        var publisher = new TestNotificationRealtimePublisher();
        var handler = new MarkNotificationReadCommandHandler(new TestNotificationRepository(notification), publisher, new TestUnitOfWork());

        var result = await handler.Handle(new MarkNotificationReadCommand(recipientUserId, notification.Id), CancellationToken.None);

        Assert.NotNull(result.ReadAtUtc);
        Assert.Equal(notification.Id, publisher.ReadNotificationId);
    }

    [Fact]
    public async Task MarkReadHandlerRejectsAUserWhoDoesNotOwnTheNotification()
    {
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.General, "Test", Guid.NewGuid(), "Test notification", null, DateTimeOffset.UtcNow);
        var handler = new MarkNotificationReadCommandHandler(new TestNotificationRepository(notification), new TestNotificationRealtimePublisher(), new TestUnitOfWork());

        await Assert.ThrowsAsync<DomainForbiddenException>(async () => await handler.Handle(new MarkNotificationReadCommand(Guid.NewGuid(), notification.Id), CancellationToken.None));

        Assert.Null(notification.ReadAtUtc);
    }

    private sealed class TestNotificationRepository(Notification notification) : INotificationRepository
    {
        public Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken = default) => Task.FromResult<Notification?>(notification.Id == notificationId ? notification : null);
        public Task<IReadOnlyList<Notification>> GetForRecipientAsync(Guid recipientUserId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Notification>>([notification]);
        public void Add(Notification newNotification) { }
        public void AddRange(IEnumerable<Notification> notifications) { }
    }

    private sealed class TestNotificationRealtimePublisher : INotificationRealtimePublisher
    {
        public Guid? ReadNotificationId { get; private set; }
        public Task PublishNotificationCreatedAsync(NotificationDto notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishNotificationReadAsync(NotificationDto notification, CancellationToken cancellationToken = default)
        {
            ReadNotificationId = notification.Id;
            return Task.CompletedTask;
        }

        public Task PublishActionItemsCreatedAsync(Guid recipientUserId, IReadOnlyCollection<ActionItemNotificationDto> actionItems, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
