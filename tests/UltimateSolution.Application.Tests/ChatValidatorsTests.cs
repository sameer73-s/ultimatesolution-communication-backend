using UltimateSolution.Application.Features.Chat;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Application.Tests;

public sealed class ChatValidatorsTests
{
    [Fact]
    public async Task CreateChannelValidatorRequiresANameForGroupChannels()
    {
        var validator = new CreateChannelCommandValidator();
        var result = await validator.ValidateAsync(new CreateChannelCommand(
            Guid.NewGuid(),
            ChatChannelType.Group,
            null,
            new[] { Guid.NewGuid() }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Name");
    }

    [Fact]
    public async Task SendChatMessageValidatorRejectsAWhitespaceOnlyBody()
    {
        var validator = new SendChatMessageCommandValidator();
        var result = await validator.ValidateAsync(new SendChatMessageCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "   "));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Body");
    }

    [Fact]
    public async Task SetPresenceStatusValidatorRejectsOfflineClientState()
    {
        var validator = new SetPresenceStatusCommandValidator();
        var result = await validator.ValidateAsync(new SetPresenceStatusCommand(
            Guid.NewGuid(),
            PresenceStatus.Offline));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Status");
    }
}
