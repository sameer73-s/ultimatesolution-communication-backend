using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;

namespace UltimateSolution.API.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<ProfileResponse>> Get()
    {
        var profile = new ProfileResponse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray());

        return Ok(ApiResponse.Ok(profile, "Profile retrieved successfully."));
    }

    public sealed record ProfileResponse(string UserId, string Email, string DisplayName, IReadOnlyCollection<string> Roles);
}
