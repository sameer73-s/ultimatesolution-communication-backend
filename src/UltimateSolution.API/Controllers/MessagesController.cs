using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;
using UltimateSolution.API.Contracts.Chat;
using UltimateSolution.Application.Features.Chat;
using UltimateSolution.Domain.Identity;

namespace UltimateSolution.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/messages")]
public sealed class MessagesController(IMediator mediator) : ControllerBase
{
    [HttpPatch("{messageId:guid}")]
    public async Task<ActionResult<ApiResponse<ChatMessageDto>>> UpdateMessage(
        Guid messageId,
        UpdateChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var message = await mediator.Send(
            new UpdateChatMessageCommand(GetCurrentUserId(), messageId, request.Body),
            cancellationToken);
        return Ok(ApiResponse.Ok(message, "Message updated successfully."));
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<ActionResult<ApiResponse<ChatMessageDto>>> DeleteMessage(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var message = await mediator.Send(
            new DeleteChatMessageCommand(GetCurrentUserId(), User.IsInRole(SystemRoles.Admin), messageId),
            cancellationToken);
        return Ok(ApiResponse.Ok(message, "Message deleted successfully."));
    }

    [HttpPost("{messageId:guid}/read")]
    public async Task<ActionResult<ApiResponse<MessageReadDto>>> MarkMessageRead(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var readState = await mediator.Send(
            new MarkMessageReadCommand(GetCurrentUserId(), messageId),
            cancellationToken);
        return Ok(ApiResponse.Ok(readState, "Message marked as read."));
    }

    private Guid GetCurrentUserId()
    {
        if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException();
    }
}
