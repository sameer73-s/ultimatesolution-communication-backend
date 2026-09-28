namespace UltimateSolution.Domain.Enums;

public enum TranscriptionJobStatus
{
    Queued = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}

public enum MeetingSummaryStatus
{
    Draft = 1,
    Approved = 2
}

public enum ActionItemStatus
{
    Open = 1, // Aka New
    InProgress = 2,
    WaitingInformation = 3,
    InReview = 4,
    Rejected = 5,
    Completed = 6,
    Cancelled = 7
}
