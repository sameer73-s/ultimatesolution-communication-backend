using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace UltimateSolution.Infrastructure.SignalR;

[Authorize]
public sealed class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.User(GetCurrentUserId()));
        await base.OnConnectedAsync();
    }

    public Task SubscribeUserNotifications() =>
        Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.User(GetCurrentUserId()));

    private Guid GetCurrentUserId() =>
        Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new HubException("An authenticated user identifier is required.");
}
