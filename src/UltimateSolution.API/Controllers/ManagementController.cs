using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;
using UltimateSolution.Domain.Identity;

namespace UltimateSolution.API.Controllers;

[ApiController]
[Route("api/v1/management")]
[Authorize(Roles = SystemRoles.Admin + "," + SystemRoles.Manager)]
public sealed class ManagementController : ControllerBase
{
    [HttpGet("ping")]
    public ActionResult<ApiResponse<string>> Ping() =>
        Ok(ApiResponse.Ok("authorized", "Manager or Admin access granted."));
}
