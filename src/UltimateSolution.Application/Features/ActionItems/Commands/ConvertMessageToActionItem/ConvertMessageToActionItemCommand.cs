using Mediator;
using UltimateSolution.Application.Common.Results;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Application.Features.ActionItems.Commands.ConvertMessageToActionItem;

public sealed record ConvertMessageToActionItemCommand(
    Guid UserId,
    Guid MessageId,
    string Title,
    Guid AssigneeUserId,
    ActionItemPriority Priority,
    DateTimeOffset? DueAtUtc
) : ICommand<Result>;
