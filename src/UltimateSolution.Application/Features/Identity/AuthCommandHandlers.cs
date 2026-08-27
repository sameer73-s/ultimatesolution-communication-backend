using Mediator;

namespace UltimateSolution.Application.Features.Identity;

public sealed class RegisterUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<RegisterUserCommand, AuthTokenResponse>
{
    public ValueTask<AuthTokenResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken) =>
        new(identityService.RegisterAsync(
            new RegisterIdentityRequest(request.Email, request.Password, request.DisplayName),
            cancellationToken));
}

public sealed class LoginUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<LoginUserCommand, AuthTokenResponse>
{
    public ValueTask<AuthTokenResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken) =>
        new(identityService.LoginAsync(request.Email, request.Password, cancellationToken));
}

public sealed class RefreshAccessTokenCommandHandler(IIdentityService identityService)
    : IRequestHandler<RefreshAccessTokenCommand, AuthTokenResponse>
{
    public ValueTask<AuthTokenResponse> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken) =>
        new(identityService.RefreshAsync(request.RefreshToken, cancellationToken));
}
