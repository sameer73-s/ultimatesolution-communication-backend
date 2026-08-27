using UltimateSolution.Domain.Enums;

namespace UltimateSolution.API.Contracts.AiSummary;

public sealed record UpdateActionItemRequest(string Title, string? Description, Guid? AssigneeUserId, DateTimeOffset? DueAtUtc, ActionItemStatus Status);
