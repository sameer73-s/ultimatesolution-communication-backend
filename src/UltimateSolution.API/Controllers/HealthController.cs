using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;

namespace UltimateSolution.API.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<HealthResponse>> Get()
    {
        var response = ApiResponse.Ok(
            new HealthResponse("UltimateSolution.Communication.API", "Healthy"),
            "API is running.");

        return Ok(response);
    }

    public sealed record HealthResponse(string Service, string Status);
}
