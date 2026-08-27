using MediatR;

namespace UltimateSolution.Application.Features.Identity;

public sealed record RegisterUserCommand(string Email, string Password, string DisplayName)
    : IRequest<AuthTokenResponse>;

public sealed record LoginUserCommand(string Email, string Password)
    : IRequest<AuthTokenResponse>;

public sealed record RefreshAccessTokenCommand(string RefreshToken)
    : IRequest<AuthTokenResponse>;

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    IReadOnlyCollection<string> Roles);

public sealed record RegisterIdentityRequest(string Email, string Password, string DisplayName);

public interface IIdentityService
{
    Task<AuthTokenResponse> RegisterAsync(
        RegisterIdentityRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthTokenResponse> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<AuthTokenResponse> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
