using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using UltimateSolution.Application.Features.Chat;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Infrastructure.SignalR;

[Authorize]
public sealed class ChatHub(IMediator mediator) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId));
        await mediator.Send(new ClientConnectedCommand(userId, Context.ConnectionId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetCurrentUserId(out var userId))
        {
            await mediator.Send(new ClientDisconnectedCommand(userId, Context.ConnectionId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeChannel(Guid channelId)
    {
        await mediator.Send(new VerifyChannelMembershipQuery(GetCurrentUserId(), channelId));
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.Channel(channelId));
    }

    public Task UnsubscribeChannel(Guid channelId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroupNames.Channel(channelId));

    public async Task SetPresence(PresenceStatus status)
    {
        await mediator.Send(new SetPresenceStatusCommand(GetCurrentUserId(), status));
    }

    public async Task StartTyping(Guid channelId)
    {
        var userId = GetCurrentUserId();
        await mediator.Send(new VerifyChannelMembershipQuery(userId, channelId));
        await Clients.OthersInGroup(HubGroupNames.Channel(channelId)).SendAsync(
            "typingChanged",
            new { channelId, userId, isTyping = true });
    }

    public async Task StopTyping(Guid channelId)
    {
        var userId = GetCurrentUserId();
        await mediator.Send(new VerifyChannelMembershipQuery(userId, channelId));
        await Clients.OthersInGroup(HubGroupNames.Channel(channelId)).SendAsync(
            "typingChanged",
            new { channelId, userId, isTyping = false });
    }

    private Guid GetCurrentUserId()
    {
        if (TryGetCurrentUserId(out var userId))
        {
            return userId;
        }

        throw new HubException("An authenticated user identifier is required.");
    }

    private bool TryGetCurrentUserId(out Guid userId) =>
        Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static string UserGroupName(Guid userId) => $"user:{userId:N}";
}
