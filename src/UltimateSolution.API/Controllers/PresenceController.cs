using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;
using UltimateSolution.Application.Features.Chat;

namespace UltimateSolution.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/presence")]
public sealed class PresenceController(IMediator mediator) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<PresenceSnapshot>>> GetPresence(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var presence = await mediator.Send(new GetPresenceQuery(userId), cancellationToken);
        return Ok(ApiResponse.Ok(presence, "Presence retrieved successfully."));
    }
}
