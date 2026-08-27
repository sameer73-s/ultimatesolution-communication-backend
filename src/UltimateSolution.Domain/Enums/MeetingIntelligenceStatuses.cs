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
    Open = 1,
    InProgress = 2,
    Completed = 3
}
