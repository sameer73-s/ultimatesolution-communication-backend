using UltimateSolution.Application.Common.Results;
using UltimateSolution.Application.Features.ActionItems.Commands.ConvertMessageToActionItem;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Chat;
using UltimateSolution.Domain.Entities.Projects;
using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Entities.Meetings;

namespace UltimateSolution.Application.Tests.Features.ActionItems;

public class ConvertMessageToActionItemHandlerTests
{
    private sealed class TestChatMessageRepository : IChatMessageRepository
    {
        public ChatMessage? Message { get; set; }
        public Task<ChatMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default) => Task.FromResult(Message);
        public Task<IReadOnlyList<ChatMessage>> GetForChannelAsync(Guid channelId, string? searchTerm, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ChatMessage>>(new List<ChatMessage>());
        public void Add(ChatMessage message) { }
    }

    private sealed class TestChatChannelRepository : IChatChannelRepository
    {
        public ChatChannel? Channel { get; set; }
        public Task<ChatChannel?> GetByIdAsync(Guid channelId, CancellationToken cancellationToken = default) => Task.FromResult(Channel);
        public Task<IReadOnlyList<ChatChannel>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ChatChannel>>(new List<ChatChannel>());
        public void Add(ChatChannel channel) { }
        public void Update(ChatChannel channel) { }
    }

    private sealed class TestActionItemRepository : IActionItemRepository
    {
        public Task<ActionItem?> GetByIdAsync(Guid actionItemId, CancellationToken cancellationToken = default) => Task.FromResult<ActionItem?>(null);
        public Task<IReadOnlyList<ActionItem>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ActionItem>>(new List<ActionItem>());
        public Task<bool> ExistsForSourceMessageAsync(Guid messageId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public void Add(ActionItem actionItem) { }
        public void AddRange(IEnumerable<ActionItem> actionItems) { }
    }

    private sealed class TestActionItemAuthorizationService : IActionItemAuthorizationService
    {
        public Task<bool> CanConvertMessageToActionItemAsync(Guid userId, ChatMessage message, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class TestOutboundNotificationService : IOutboundNotificationService
    {
        public Task<Result> SendAsync(OutboundNotificationRequest request, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
    }

    private sealed class TestProjectRepository : IProjectRepository
    {
        public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<Project?>(null);
        public Task<IReadOnlyList<ProjectMember>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ProjectMember>>(new List<ProjectMember>());
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new Exception("DbUpdateException") { Source = "Test" };
        }
    }

    // A custom exception to simulate EF Core DbUpdateException
    private class DummyDbUpdateException : Exception
    {
        public DummyDbUpdateException(string message, Exception innerException) : base(message, innerException) { }
    }

    [Fact]
    public async Task Handle_WhenConcurrencyCausesConstraintViolation_ShouldCatchExceptionAndReturnConflict()
    {
        // Arrange
        var messageRepository = new TestChatMessageRepository();
        var channelRepository = new TestChatChannelRepository();
        var actionItemRepository = new TestActionItemRepository();
        var authorizationService = new TestActionItemAuthorizationService();
        var notificationService = new TestOutboundNotificationService();
        var projectRepository = new TestProjectRepository();

        // Create an IUnitOfWork that throws a fake DbUpdateException
        var unitOfWork = new FakeFailingUnitOfWork();

        var handler = new ConvertMessageToActionItemHandler(
            messageRepository,
            channelRepository,
            actionItemRepository,
            authorizationService,
            notificationService,
            projectRepository,
            unitOfWork
        );

        var request = new ConvertMessageToActionItemCommand(Guid.NewGuid(), Guid.NewGuid(), "Task", Guid.NewGuid(), ActionItemPriority.Medium, null);
        messageRepository.Message = ChatMessage.Create(Guid.NewGuid(), request.UserId, "Hello");
        channelRepository.Channel = ChatChannel.Create(Guid.NewGuid(), "Channel", ChatChannelType.Group);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ActionItem.AlreadyConverted", result.ErrorCode);
    }

    private sealed class FakeFailingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var innerEx = new Exception("IX_ActionItems_SourceMessageId");
            // By dynamically creating an exception with the required name, we can bypass not having EF Core reference
            // But the easiest way is to use a class named DbUpdateException in our test context, or just mock it.
            // Wait, the handler does ex.GetType().Name == "DbUpdateException". So we must name it DbUpdateException!
            throw new DbUpdateException("DB Error", innerEx);
        }
    }
}

// Defining a class named DbUpdateException in the global or same namespace will satisfy ex.GetType().Name == "DbUpdateException"
public class DbUpdateException : Exception
{
    public DbUpdateException(string message, Exception innerException) : base(message, innerException) { }
}
