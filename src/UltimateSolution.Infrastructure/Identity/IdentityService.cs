using Microsoft.AspNetCore.Identity;
using UltimateSolution.Application.Features.Identity;
using UltimateSolution.Domain.Exceptions;
using UltimateSolution.Domain.Identity;

namespace UltimateSolution.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    JwtTokenService jwtTokenService)
    : IIdentityService
{
    public async Task<AuthTokenResponse> RegisterAsync(
        RegisterIdentityRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = false
        };
        var createResult = await userManager.CreateAsync(user, request.Password);
        EnsureSucceeded(createResult);

        var roleResult = await userManager.AddToRoleAsync(user, SystemRoles.Employee);
        EnsureSucceeded(roleResult);

        return await jwtTokenService.CreateTokenPairAsync(user, cancellationToken);
    }

    public async Task<AuthTokenResponse> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            throw new UnauthorizedAccessException();
        }

        return await jwtTokenService.CreateTokenPairAsync(user, cancellationToken);
    }

    public Task<AuthTokenResponse> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default) =>
        jwtTokenService.RefreshAsync(refreshToken, cancellationToken);

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new DomainValidationException(errors);
        }
    }
}
