using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;
using UltimateSolution.API.Contracts.Auth;
using UltimateSolution.Application.Features.Identity;

namespace UltimateSolution.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await sender.Send(
            new RegisterUserCommand(request.Email, request.Password, request.DisplayName),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(tokens, "User registered successfully."));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await sender.Send(new LoginUserCommand(request.Email, request.Password), cancellationToken);
        return Ok(ApiResponse.Ok(tokens, "Login completed successfully."));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await sender.Send(new RefreshAccessTokenCommand(request.RefreshToken), cancellationToken);
        return Ok(ApiResponse.Ok(tokens, "Token refreshed successfully."));
    }
}
