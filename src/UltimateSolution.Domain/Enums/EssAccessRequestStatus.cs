namespace UltimateSolution.Domain.Enums;

public enum EssAccessRequestStatus
{
    PendingManager = 1,
    RejectedByManager = 2,
    PendingHR = 3,
    NeedsInformation = 4,
    Enabled = 5,
    Closed = 6
}
