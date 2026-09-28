using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Domain.Entities.Chat;

public sealed class MessageDeletionRequest
{
    private MessageDeletionRequest()
    {
    }

    private MessageDeletionRequest(Guid messageId, Guid requestedByUserId, Guid? secondPartyUserId, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        RequestedByUserId = requestedByUserId;
        SecondPartyUserId = secondPartyUserId;
        CreatedAtUtc = createdAtUtc;
        Status = MessageDeletionRequestStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public Guid? SecondPartyUserId { get; private set; }
    public MessageDeletionRequestStatus Status { get; private set; }
    public string? RestoreReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? RespondedAtUtc { get; private set; }

    public static MessageDeletionRequest Create(Guid messageId, Guid requestedByUserId, Guid? secondPartyUserId, DateTimeOffset createdAtUtc)
    {
        if (messageId == Guid.Empty || requestedByUserId == Guid.Empty)
        {
            throw new DomainValidationException("Message and Requesting user are required.");
        }

        return new MessageDeletionRequest(messageId, requestedByUserId, secondPartyUserId, createdAtUtc);
    }

    public void Approve(Guid approvedByUserId, DateTimeOffset respondedAtUtc)
    {
        if (Status != MessageDeletionRequestStatus.Pending)
        {
            throw new DomainValidationException("Only pending deletion requests can be approved.");
        }

        if (SecondPartyUserId.HasValue && approvedByUserId != SecondPartyUserId.Value)
        {
            throw new DomainValidationException("Only the designated second party can approve this request.");
        }

        Status = MessageDeletionRequestStatus.Approved;
        RespondedAtUtc = respondedAtUtc;
    }

    public void Reject(Guid rejectedByUserId, DateTimeOffset respondedAtUtc)
    {
        if (Status != MessageDeletionRequestStatus.Pending)
        {
            throw new DomainValidationException("Only pending deletion requests can be rejected.");
        }

        if (SecondPartyUserId.HasValue && rejectedByUserId != SecondPartyUserId.Value)
        {
            throw new DomainValidationException("Only the designated second party can reject this request.");
        }

        Status = MessageDeletionRequestStatus.Rejected;
        RespondedAtUtc = respondedAtUtc;
    }

    public void Restore(string restoreReason, DateTimeOffset respondedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(restoreReason))
        {
            throw new DomainValidationException("A restore reason is required for administrative restores.");
        }

        Status = MessageDeletionRequestStatus.Restored;
        RestoreReason = restoreReason.Trim();
        RespondedAtUtc = respondedAtUtc;
    }
}
