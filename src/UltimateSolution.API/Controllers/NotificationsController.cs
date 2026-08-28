using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;
using UltimateSolution.Application.Features.Notifications;

namespace UltimateSolution.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<NotificationDto>>>> GetNotifications(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await mediator.Send(new GetNotificationsQuery(GetCurrentUserId()), cancellationToken), "Notifications retrieved successfully."));

    [HttpPost("{notificationId:guid}/read")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkRead(Guid notificationId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await mediator.Send(new MarkNotificationReadCommand(GetCurrentUserId(), notificationId), cancellationToken), "Notification marked as read."));

    private Guid GetCurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException();
}
