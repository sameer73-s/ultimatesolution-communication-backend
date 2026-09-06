using UltimateSolution.Application.Common.Results;
using UltimateSolution.Application.Features.AiSummary;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Meetings;
using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Application.Tests;

public sealed class AiSummaryApprovalHandlerTests
{
    [Fact]
    public async Task ApproveHandlerUsesPolicyAndCreatesConfirmedActionItemsOnlyAfterAuthorization()
    {
        var organizerUserId = Guid.NewGuid();
        var attendeeUserId = Guid.NewGuid();
        var meeting = Meeting.Schedule("Review", null, organizerUserId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        meeting.AddParticipant(organizerUserId, MeetingParticipantRole.Organizer, DateTimeOffset.UtcNow);
        meeting.AddParticipant(attendeeUserId, MeetingParticipantRole.Attendee, DateTimeOffset.UtcNow);
        var transcriptionJob = TranscriptionJob.Queue(meeting.Id, Guid.NewGuid(), "recording-reference", DateTimeOffset.UtcNow);
        var summary = MeetingSummary.CreateDraft(
            meeting.Id,
            transcriptionJob.Id,
            "Review summary",
            "[\"Approve the release plan.\"]",
            "[{\"title\":\"Prepare the release plan\",\"description\":null,\"assigneeUserId\":\"" + attendeeUserId + "\",\"dueAtUtc\":null}]",
            "test-summary",
            DateTimeOffset.UtcNow);
        var actionItems = new TestActionItemRepository();
        var notifications = new TestNotificationRepository();
        var handler = new ApproveMeetingSummaryCommandHandler(
            new TestMeetingRepository(meeting),
            new TestMeetingIntelligenceRepository(summary),
            actionItems,
            notifications,
            new TestNotificationRealtimePublisher(),
            new AllowApprovalPolicy(),
            new TestUnitOfWork());

        var result = await handler.Handle(new ApproveMeetingSummaryCommand(organizerUserId, meeting.Id), CancellationToken.None);

        Assert.Equal(MeetingSummaryStatus.Approved, result.Status);
        Assert.Equal(organizerUserId, result.ApprovedByUserId);
        var actionItem = Assert.Single(actionItems.Items);
        Assert.Equal(summary.Id, actionItem.MeetingSummaryId);
        Assert.Equal(attendeeUserId, actionItem.AssigneeUserId);
        Assert.Single(notifications.Items);
    }

    [Fact]
    public async Task ApproveHandlerRejectsWhenTheExtensiblePolicyDoesNotAuthorize()
    {
        var organizerUserId = Guid.NewGuid();
        var meeting = Meeting.Schedule("Review", null, organizerUserId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        meeting.AddParticipant(organizerUserId, MeetingParticipantRole.Organizer, DateTimeOffset.UtcNow);
        var summary = MeetingSummary.CreateDraft(meeting.Id, Guid.NewGuid(), "Review summary", "[]", "[]", null, DateTimeOffset.UtcNow);
        var actionItems = new TestActionItemRepository();
        var notifications = new TestNotificationRepository();
        var handler = new ApproveMeetingSummaryCommandHandler(
            new TestMeetingRepository(meeting),
            new TestMeetingIntelligenceRepository(summary),
            actionItems,
            notifications,
            new TestNotificationRealtimePublisher(),
            new DenyApprovalPolicy(),
            new TestUnitOfWork());

        await Assert.ThrowsAsync<DomainForbiddenException>(async () => await handler.Handle(new ApproveMeetingSummaryCommand(organizerUserId, meeting.Id), CancellationToken.None));

        Assert.Equal(MeetingSummaryStatus.Draft, summary.Status);
        Assert.Empty(actionItems.Items);
        Assert.Empty(notifications.Items);
    }

    private sealed class TestMeetingRepository(Meeting meeting) : IMeetingRepository
    {
        public Task<Meeting?> GetByIdAsync(Guid meetingId, CancellationToken cancellationToken = default) => Task.FromResult<Meeting?>(meeting.Id == meetingId ? meeting : null);
        public Task<IReadOnlyList<Meeting>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Meeting>>([meeting]);
        public void Add(Meeting newMeeting) { }
        public void AddRecording(MeetingRecording recording) { }
    }

    private sealed class TestMeetingIntelligenceRepository(MeetingSummary summary) : IMeetingIntelligenceRepository
    {
        public Task<MeetingRecording?> GetRecordingByIdAsync(Guid recordingId, CancellationToken cancellationToken = default) => Task.FromResult<MeetingRecording?>(null);
        public Task<TranscriptionJob?> GetLatestTranscriptionJobAsync(Guid meetingId, CancellationToken cancellationToken = default) => Task.FromResult<TranscriptionJob?>(null);
        public Task<TranscriptionJob?> GetLatestCompletedTranscriptionJobAsync(Guid meetingId, CancellationToken cancellationToken = default) => Task.FromResult<TranscriptionJob?>(null);
        public Task<MeetingSummary?> GetLatestSummaryAsync(Guid meetingId, CancellationToken cancellationToken = default) => Task.FromResult<MeetingSummary?>(summary.MeetingId == meetingId ? summary : null);
        public void AddTranscriptionJob(TranscriptionJob transcriptionJob) { }
        public void AddMeetingSummary(MeetingSummary meetingSummary) { }
    }

    private sealed class TestActionItemRepository : IActionItemRepository
    {
        public List<ActionItem> Items { get; } = [];
        public Task<ActionItem?> GetByIdAsync(Guid actionItemId, CancellationToken cancellationToken = default) => Task.FromResult<ActionItem?>(Items.SingleOrDefault(actionItem => actionItem.Id == actionItemId));
        public Task<IReadOnlyList<ActionItem>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ActionItem>>(Items.Where(actionItem => actionItem.AssigneeUserId == userId).ToArray());
        public Task<bool> ExistsForSourceMessageAsync(Guid messageId, CancellationToken cancellationToken = default) => Task.FromResult(Items.Any(actionItem => actionItem.SourceMessageId == messageId));
        public void Add(ActionItem actionItem) => Items.Add(actionItem);
        public void AddRange(IEnumerable<ActionItem> actionItems) => Items.AddRange(actionItems);
    }

    private sealed class TestNotificationRepository : INotificationRepository
    {
        public List<UltimateSolution.Domain.Entities.Notifications.Notification> Items { get; } = [];
        public Task<UltimateSolution.Domain.Entities.Notifications.Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken = default) => Task.FromResult<UltimateSolution.Domain.Entities.Notifications.Notification?>(Items.SingleOrDefault(notification => notification.Id == notificationId));
        public Task<IReadOnlyList<UltimateSolution.Domain.Entities.Notifications.Notification>> GetForRecipientAsync(Guid recipientUserId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<UltimateSolution.Domain.Entities.Notifications.Notification>>(Items.Where(notification => notification.RecipientUserId == recipientUserId).ToArray());
        public void Add(UltimateSolution.Domain.Entities.Notifications.Notification notification) => Items.Add(notification);
        public void AddRange(IEnumerable<UltimateSolution.Domain.Entities.Notifications.Notification> notifications) => Items.AddRange(notifications);
    }

    private sealed class TestNotificationRealtimePublisher : INotificationRealtimePublisher
    {
        public Task PublishNotificationCreatedAsync(UltimateSolution.Application.Features.Notifications.NotificationDto notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishNotificationReadAsync(UltimateSolution.Application.Features.Notifications.NotificationDto notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishActionItemsCreatedAsync(Guid recipientUserId, IReadOnlyCollection<UltimateSolution.Application.Features.Notifications.ActionItemNotificationDto> actionItems, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class AllowApprovalPolicy : IMeetingSummaryApprovalPolicy
    {
        public Task<Result> AuthorizeAsync(MeetingSummaryApprovalAuthorizationRequest request, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    private sealed class DenyApprovalPolicy : IMeetingSummaryApprovalPolicy
    {
        public Task<Result> AuthorizeAsync(MeetingSummaryApprovalAuthorizationRequest request, CancellationToken cancellationToken) => Task.FromResult(Result.Failure("denied"));
    }
}
