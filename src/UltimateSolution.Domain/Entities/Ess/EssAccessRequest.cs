using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Domain.Entities.Ess;

public sealed class EssAccessRequest
{
    private EssAccessRequest()
    {
    }

    private EssAccessRequest(Guid employeeUserId, Guid managerUserId, string requestedServiceType, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        EmployeeUserId = employeeUserId;
        ManagerUserId = managerUserId;
        RequestedServiceType = requestedServiceType;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        Status = EssAccessRequestStatus.PendingManager;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeUserId { get; private set; }
    public Guid ManagerUserId { get; private set; }
    public Guid? HrAssigneeUserId { get; private set; }
    public string RequestedServiceType { get; private set; } = string.Empty;
    public string? EssServiceReference { get; private set; }
    public EssAccessRequestStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public static EssAccessRequest Create(Guid employeeUserId, Guid managerUserId, string requestedServiceType, DateTimeOffset createdAtUtc)
    {
        if (employeeUserId == Guid.Empty || managerUserId == Guid.Empty)
        {
            throw new DomainValidationException("Employee and Manager users are required.");
        }

        if (string.IsNullOrWhiteSpace(requestedServiceType))
        {
            throw new DomainValidationException("Requested service type is required.");
        }

        return new EssAccessRequest(employeeUserId, managerUserId, requestedServiceType.Trim(), createdAtUtc);
    }

    public void ApproveByManager(DateTimeOffset updatedAtUtc)
    {
        if (Status != EssAccessRequestStatus.PendingManager)
        {
            throw new DomainValidationException("Only requests pending manager approval can be approved by manager.");
        }

        Status = EssAccessRequestStatus.PendingHR;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void RejectByManager(DateTimeOffset updatedAtUtc)
    {
        if (Status != EssAccessRequestStatus.PendingManager)
        {
            throw new DomainValidationException("Only requests pending manager approval can be rejected.");
        }

        Status = EssAccessRequestStatus.RejectedByManager;
        UpdatedAtUtc = updatedAtUtc;
        ClosedAtUtc = updatedAtUtc;
    }

    public void RequestInformation(Guid hrAssigneeUserId, DateTimeOffset updatedAtUtc)
    {
        if (Status is not (EssAccessRequestStatus.PendingHR or EssAccessRequestStatus.NeedsInformation))
        {
            throw new DomainValidationException("Information can only be requested for active HR requests.");
        }

        HrAssigneeUserId = hrAssigneeUserId;
        Status = EssAccessRequestStatus.NeedsInformation;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Enable(Guid hrAssigneeUserId, string essServiceReference, DateTimeOffset updatedAtUtc)
    {
        if (Status is not (EssAccessRequestStatus.PendingHR or EssAccessRequestStatus.NeedsInformation))
        {
            throw new DomainValidationException("Only active HR requests can be enabled.");
        }

        if (string.IsNullOrWhiteSpace(essServiceReference))
        {
            throw new DomainValidationException("ESS service reference is required when enabling.");
        }

        HrAssigneeUserId = hrAssigneeUserId;
        EssServiceReference = essServiceReference.Trim();
        Status = EssAccessRequestStatus.Enabled;
        UpdatedAtUtc = updatedAtUtc;
        ClosedAtUtc = updatedAtUtc;
    }
}
