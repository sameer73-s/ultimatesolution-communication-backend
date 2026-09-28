using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;
using UltimateSolution.API.Contracts.AiSummary;
using UltimateSolution.Application.Features.AiSummary;
using UltimateSolution.Domain.Identity;
using UltimateSolution.API.Contracts.ActionItems;
using UltimateSolution.Application.Features.ActionItems.Commands.ConvertMessageToActionItem;

namespace UltimateSolution.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/action-items")]
public sealed class ActionItemsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ActionItemDto>>>> GetActionItems(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await mediator.Send(new GetActionItemsQuery(GetCurrentUserId()), cancellationToken), "Action items retrieved successfully."));

    [HttpPatch("{actionItemId:guid}")]
    public async Task<ActionResult<ApiResponse<ActionItemDto>>> UpdateActionItem(Guid actionItemId, UpdateActionItemRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await mediator.Send(new UpdateActionItemCommand(GetCurrentUserId(), IsManager(), actionItemId, request.Title, request.Description, request.AssigneeUserId, request.DueAtUtc, request.Status), cancellationToken), "Action item updated successfully."));

    [HttpPost("~/api/v1/messages/{messageId}/action-items")]
    public async Task<ActionResult<ApiResponse<string>>> ConvertMessageToActionItem(Guid messageId, ConvertMessageToActionItemRequest request, CancellationToken cancellationToken)
    {
        var command = new ConvertMessageToActionItemCommand(
            GetCurrentUserId(),
            messageId,
            request.Title,
            request.AssigneeUserId,
            request.Priority,
            request.DueAtUtc
        );

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "ActionItem.AlreadyConverted") return Conflict(ApiResponse.Failure<string>(result.ErrorCode));
            if (result.ErrorCode == "ActionItem.Unauthorized") return Forbid();
            if (result.ErrorCode == "Message.NotFound" || result.ErrorCode == "Channel.NotFound") return NotFound(ApiResponse.Failure<string>(result.ErrorCode));
            return BadRequest(ApiResponse.Failure<string>(result.ErrorCode ?? "UnknownError"));
        }

        return Ok(ApiResponse.Ok<string>("Action item created successfully from message."));
    }

    private bool IsManager() => User.IsInRole(SystemRoles.Manager) || User.IsInRole(SystemRoles.Admin);
    private Guid GetCurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : throw new UnauthorizedAccessException();
}
