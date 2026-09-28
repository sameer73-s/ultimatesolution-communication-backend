using UltimateSolution.Domain.Enums;

namespace UltimateSolution.API.Contracts.ActionItems;

public sealed record ConvertMessageToActionItemRequest(
    string Title,
    Guid AssigneeUserId,
    ActionItemPriority Priority = ActionItemPriority.Medium,
    DateTimeOffset? DueAtUtc = null
);
