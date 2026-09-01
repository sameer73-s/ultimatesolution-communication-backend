using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Domain.Entities.Meetings;

public sealed class TranscriptionJob
{
    private TranscriptionJob()
    {
    }

    private TranscriptionJob(Guid meetingId, Guid recordingId, string mediaRecordingReference, DateTimeOffset requestedAtUtc)
    {
        Id = Guid.NewGuid();
        MeetingId = meetingId;
        RecordingId = recordingId;
        MediaRecordingReference = mediaRecordingReference;
        RequestedAtUtc = requestedAtUtc;
        Status = TranscriptionJobStatus.Queued;
    }

    public Guid Id { get; private set; }
    public Guid MeetingId { get; private set; }
    public Guid RecordingId { get; private set; }
    public string MediaRecordingReference { get; private set; } = string.Empty;
    public string? ExternalJobReference { get; private set; }
    public TranscriptionJobStatus Status { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? FailureCode { get; private set; }
    public ICollection<TranscriptionSegment> Segments { get; } = new List<TranscriptionSegment>();

    public static TranscriptionJob Queue(Guid meetingId, Guid recordingId, string mediaRecordingReference, DateTimeOffset requestedAtUtc)
    {
        if (meetingId == Guid.Empty || recordingId == Guid.Empty)
        {
            throw new DomainValidationException("A meeting and recording are required to request transcription.");
        }

        if (string.IsNullOrWhiteSpace(mediaRecordingReference))
        {
            throw new DomainValidationException("A media recording reference is required to request transcription.");
        }

        return new TranscriptionJob(meetingId, recordingId, mediaRecordingReference.Trim(), requestedAtUtc);
    }

    public void MarkProcessing(string externalJobReference)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(externalJobReference))
        {
            throw new DomainValidationException("An external transcription job reference is required.");
        }

        ExternalJobReference = externalJobReference.Trim();
        Status = TranscriptionJobStatus.Processing;
    }

    public void Complete(IEnumerable<TranscriptionSegment> segments, DateTimeOffset completedAtUtc)
    {
        if (Status is not (TranscriptionJobStatus.Queued or TranscriptionJobStatus.Processing))
        {
            throw new DomainValidationException("Only a pending transcription job can be completed.");
        }

        var orderedSegments = segments.OrderBy(segment => segment.SequenceNumber).ToArray();
        if (orderedSegments.Length == 0)
        {
            throw new DomainValidationException("A completed transcription requires at least one segment.");
        }

        if (orderedSegments.Any(segment => segment.TranscriptionJobId != Id))
        {
            throw new DomainValidationException("Every transcription segment must belong to the transcription job.");
        }

        if (orderedSegments.Select(segment => segment.SequenceNumber).Distinct().Count() != orderedSegments.Length)
        {
            throw new DomainValidationException("Transcription segment sequence numbers must be unique.");
        }

        foreach (var segment in orderedSegments)
        {
            Segments.Add(segment);
        }

        Status = TranscriptionJobStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        FailureCode = null;
    }

    public void Fail(string failureCode)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new DomainValidationException("A transcription failure code is required.");
        }

        Status = TranscriptionJobStatus.Failed;
        FailureCode = failureCode.Trim();
    }

    private void EnsurePending()
    {
        if (Status is not (TranscriptionJobStatus.Queued or TranscriptionJobStatus.Processing))
        {
            throw new DomainValidationException("The transcription job is no longer pending.");
        }
    }
}

public sealed class TranscriptionSegment
{
    private TranscriptionSegment()
    {
    }

    public TranscriptionSegment(Guid transcriptionJobId, int sequenceNumber, string text, string? speakerLabel, TimeSpan startOffset, TimeSpan endOffset)
    {
        if (transcriptionJobId == Guid.Empty || sequenceNumber <= 0)
        {
            throw new DomainValidationException("A transcription job and positive sequence number are required.");
        }

        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > 4000)
        {
            throw new DomainValidationException("Transcription segment text is required and cannot exceed 4000 characters.");
        }

        if (speakerLabel?.Length > 120)
        {
            throw new DomainValidationException("Transcription speaker label cannot exceed 120 characters.");
        }

        if (startOffset < TimeSpan.Zero || endOffset < startOffset)
        {
            throw new DomainValidationException("Transcription segment offsets are invalid.");
        }

        Id = Guid.NewGuid();
        TranscriptionJobId = transcriptionJobId;
        SequenceNumber = sequenceNumber;
        Text = text.Trim();
        SpeakerLabel = string.IsNullOrWhiteSpace(speakerLabel) ? null : speakerLabel.Trim();
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    public Guid Id { get; private set; }
    public Guid TranscriptionJobId { get; private set; }
    public int SequenceNumber { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string? SpeakerLabel { get; private set; }
    public TimeSpan StartOffset { get; private set; }
    public TimeSpan EndOffset { get; private set; }
}

public sealed class MeetingSummary
{
    private MeetingSummary()
    {
    }

    private MeetingSummary(Guid meetingId, Guid transcriptionJobId, string content, string decisionsJson, string proposedActionItemsJson, string? externalSummaryReference, DateTimeOffset generatedAtUtc)
    {
        Id = Guid.NewGuid();
        MeetingId = meetingId;
        TranscriptionJobId = transcriptionJobId;
        Content = content;
        DecisionsJson = decisionsJson;
        ProposedActionItemsJson = proposedActionItemsJson;
        ExternalSummaryReference = externalSummaryReference;
        GeneratedAtUtc = generatedAtUtc;
        Status = MeetingSummaryStatus.Draft;
    }

    public Guid Id { get; private set; }
    public Guid MeetingId { get; private set; }
    public Guid TranscriptionJobId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string DecisionsJson { get; private set; } = "[]";
    public string ProposedActionItemsJson { get; private set; } = "[]";
    public string? ExternalSummaryReference { get; private set; }
    public MeetingSummaryStatus Status { get; private set; }
    public DateTimeOffset GeneratedAtUtc { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public ICollection<ActionItem> ActionItems { get; } = new List<ActionItem>();

    public static MeetingSummary CreateDraft(Guid meetingId, Guid transcriptionJobId, string content, string decisionsJson, string proposedActionItemsJson, string? externalSummaryReference, DateTimeOffset generatedAtUtc)
    {
        if (meetingId == Guid.Empty || transcriptionJobId == Guid.Empty)
        {
            throw new DomainValidationException("A meeting and completed transcription are required for a summary.");
        }

        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length > 16000)
        {
            throw new DomainValidationException("Summary content is required and cannot exceed 16000 characters.");
        }

        if (string.IsNullOrWhiteSpace(decisionsJson) || decisionsJson.Length > 16000 || string.IsNullOrWhiteSpace(proposedActionItemsJson) || proposedActionItemsJson.Length > 32000)
        {
            throw new DomainValidationException("Summary decisions and proposed action items are required.");
        }

        if (externalSummaryReference?.Length > 200)
        {
            throw new DomainValidationException("External summary reference cannot exceed 200 characters.");
        }

        return new MeetingSummary(meetingId, transcriptionJobId, content.Trim(), decisionsJson, proposedActionItemsJson, string.IsNullOrWhiteSpace(externalSummaryReference) ? null : externalSummaryReference.Trim(), generatedAtUtc);
    }

    public void Approve(Guid approvedByUserId, DateTimeOffset approvedAtUtc)
    {
        if (Status != MeetingSummaryStatus.Draft)
        {
            throw new DomainValidationException("Only a draft meeting summary can be approved.");
        }

        if (approvedByUserId == Guid.Empty)
        {
            throw new DomainValidationException("An approving user is required.");
        }

        Status = MeetingSummaryStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = approvedAtUtc;
    }
}

public sealed class ActionItem
{
    private ActionItem()
    {
    }

    private ActionItem(
        ActionItemSourceType sourceType,
        Guid? meetingId,
        Guid? meetingSummaryId,
        Guid? sourceMessageId,
        Guid? projectId,
        string title,
        string? description,
        Guid? assigneeUserId,
        Guid? reviewerUserId,
        ActionItemPriority priority,
        DateTimeOffset? dueAtUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        SourceType = sourceType;
        MeetingId = meetingId;
        MeetingSummaryId = meetingSummaryId;
        SourceMessageId = sourceMessageId;
        ProjectId = projectId;
        Title = title;
        Description = description;
        AssigneeUserId = assigneeUserId;
        ReviewerUserId = reviewerUserId;
        Priority = priority;
        DueAtUtc = dueAtUtc;
        CreatedAtUtc = createdAtUtc;
        Status = ActionItemStatus.Open;
    }

    public Guid Id { get; private set; }
    public ActionItemSourceType SourceType { get; private set; }
    public Guid? MeetingId { get; private set; }
    public Guid? MeetingSummaryId { get; private set; }
    public Guid? SourceMessageId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? AssigneeUserId { get; private set; }
    public Guid? ReviewerUserId { get; private set; }
    public ActionItemPriority Priority { get; private set; }
    public DateTimeOffset? DueAtUtc { get; private set; }
    public ActionItemStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static ActionItem Create(Guid meetingId, Guid meetingSummaryId, string title, string? description, Guid? assigneeUserId, DateTimeOffset? dueAtUtc, DateTimeOffset createdAtUtc)
    {
        if (meetingId == Guid.Empty || meetingSummaryId == Guid.Empty)
        {
            throw new DomainValidationException("A meeting and approved summary are required for an action item.");
        }

        ValidateCommon(title, description);

        return new ActionItem(ActionItemSourceType.Meeting, meetingId, meetingSummaryId, null, null, title.Trim(), string.IsNullOrWhiteSpace(description) ? null : description.Trim(), assigneeUserId, null, ActionItemPriority.Medium, dueAtUtc, createdAtUtc);
    }

    public static ActionItem CreateFromMessage(Guid sourceMessageId, Guid? projectId, string title, string? description, Guid? assigneeUserId, Guid? reviewerUserId, ActionItemPriority priority, DateTimeOffset? dueAtUtc, DateTimeOffset createdAtUtc)
    {
        if (sourceMessageId == Guid.Empty)
        {
            throw new DomainValidationException("A source message is required for a message-based action item.");
        }

        ValidateCommon(title, description);

        return new ActionItem(ActionItemSourceType.Message, null, null, sourceMessageId, projectId, title.Trim(), string.IsNullOrWhiteSpace(description) ? null : description.Trim(), assigneeUserId, reviewerUserId, priority, dueAtUtc, createdAtUtc);
    }

    private static void ValidateCommon(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 400)
        {
            throw new DomainValidationException("Action item title is required and cannot exceed 400 characters.");
        }

        if (description?.Length > 4000)
        {
            throw new DomainValidationException("Action item description cannot exceed 4000 characters.");
        }
    }

    public void Update(string title, string? description, Guid? assigneeUserId, DateTimeOffset? dueAtUtc, ActionItemStatus status, DateTimeOffset updatedAtUtc)
    {
        ValidateCommon(title, description);

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        AssigneeUserId = assigneeUserId;
        DueAtUtc = dueAtUtc;
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
    }
}

public sealed class ActionItemHistory
{
    private ActionItemHistory()
    {
    }

    public ActionItemHistory(Guid actionItemId, Guid changedByUserId, ActionItemStatus oldStatus, ActionItemStatus newStatus, string? comment, DateTimeOffset changedAtUtc)
    {
        Id = Guid.NewGuid();
        ActionItemId = actionItemId;
        ChangedByUserId = changedByUserId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        ChangedAtUtc = changedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid ActionItemId { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public ActionItemStatus OldStatus { get; private set; }
    public ActionItemStatus NewStatus { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
}
