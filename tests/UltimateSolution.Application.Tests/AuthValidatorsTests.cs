using UltimateSolution.Application.Features.Identity;

namespace UltimateSolution.Application.Tests;

public sealed class AuthValidatorsTests
{
    [Fact]
    public async Task RegisterValidatorAcceptsACompliantPassword()
    {
        var validator = new RegisterUserCommandValidator();
        var result = await validator.ValidateAsync(
            new RegisterUserCommand("employee@ultimatesolution.test", "StrongPassword!2026", "Employee"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RegisterValidatorRejectsAnUnsafePassword()
    {
        var validator = new RegisterUserCommandValidator();
        var result = await validator.ValidateAsync(
            new RegisterUserCommand("employee@ultimatesolution.test", "unsafe", "Employee"));

        Assert.False(result.IsValid);
    }
}
