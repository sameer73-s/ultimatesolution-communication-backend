using MediatR;

namespace UltimateSolution.Application.Features.Identity;

public sealed class RegisterUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<RegisterUserCommand, AuthTokenResponse>
{
    public Task<AuthTokenResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken) =>
        identityService.RegisterAsync(
            new RegisterIdentityRequest(request.Email, request.Password, request.DisplayName),
            cancellationToken);
}

public sealed class LoginUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<LoginUserCommand, AuthTokenResponse>
{
    public Task<AuthTokenResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken) =>
        identityService.LoginAsync(request.Email, request.Password, cancellationToken);
}

public sealed class RefreshAccessTokenCommandHandler(IIdentityService identityService)
    : IRequestHandler<RefreshAccessTokenCommand, AuthTokenResponse>
{
    public Task<AuthTokenResponse> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken) =>
        identityService.RefreshAsync(request.RefreshToken, cancellationToken);
}
