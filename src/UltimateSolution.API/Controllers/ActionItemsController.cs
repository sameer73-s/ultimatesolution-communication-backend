using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;
using UltimateSolution.API.Contracts.AiSummary;
using UltimateSolution.Application.Features.AiSummary;
using UltimateSolution.Domain.Identity;

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

    private bool IsManager() => User.IsInRole(SystemRoles.Manager) || User.IsInRole(SystemRoles.Admin);
    private Guid GetCurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : throw new UnauthorizedAccessException();
}
