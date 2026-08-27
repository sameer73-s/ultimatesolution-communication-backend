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
[Route("api/v1/channels")]
public sealed class ChannelsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ChannelDto>>>> GetChannels(
        CancellationToken cancellationToken)
    {
        var channels = await mediator.Send(new GetChannelsQuery(GetCurrentUserId()), cancellationToken);
        return Ok(ApiResponse.Ok(channels, "Channels retrieved successfully."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ChannelDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ChannelDto>>> CreateChannel(
        CreateChannelRequest request,
        CancellationToken cancellationToken)
    {
        var channel = await mediator.Send(
            new CreateChannelCommand(GetCurrentUserId(), request.Type, request.Name, request.MemberIds),
            cancellationToken);
        return CreatedAtAction(
            nameof(GetChannel),
            new { channelId = channel.Id },
            ApiResponse.Ok(channel, "Channel created successfully."));
    }

    [HttpGet("{channelId:guid}")]
    public async Task<ActionResult<ApiResponse<ChannelDto>>> GetChannel(
        Guid channelId,
        CancellationToken cancellationToken)
    {
        var channel = await mediator.Send(new GetChannelQuery(GetCurrentUserId(), channelId), cancellationToken);
        return Ok(ApiResponse.Ok(channel, "Channel retrieved successfully."));
    }

    [HttpPost("{channelId:guid}/members")]
    public async Task<ActionResult<ApiResponse<ChannelDto>>> AddMember(
        Guid channelId,
        AddChannelMemberRequest request,
        CancellationToken cancellationToken)
    {
        var channel = await mediator.Send(
            new AddChannelMemberCommand(
                GetCurrentUserId(),
                User.IsInRole(SystemRoles.Admin),
                channelId,
                request.UserId),
            cancellationToken);
        return Ok(ApiResponse.Ok(channel, "Channel member added successfully."));
    }

    [HttpDelete("{channelId:guid}/members/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<ChannelDto>>> RemoveMember(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var channel = await mediator.Send(
            new RemoveChannelMemberCommand(
                GetCurrentUserId(),
                User.IsInRole(SystemRoles.Admin),
                channelId,
                userId),
            cancellationToken);
        return Ok(ApiResponse.Ok(channel, "Channel member removed successfully."));
    }

    [HttpPatch("{channelId:guid}")]
    public async Task<ActionResult<ApiResponse<ChannelDto>>> UpdateChannel(
        Guid channelId,
        UpdateChannelRequest request,
        CancellationToken cancellationToken)
    {
        var channel = await mediator.Send(
            new UpdateChannelCommand(
                GetCurrentUserId(),
                User.IsInRole(SystemRoles.Admin),
                channelId,
                request.Name,
                request.IsArchived),
            cancellationToken);
        return Ok(ApiResponse.Ok(channel, "Channel updated successfully."));
    }

    [HttpGet("{channelId:guid}/messages")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ChatMessageDto>>>> GetMessages(
        Guid channelId,
        [FromQuery] string? search,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var messages = await mediator.Send(
            new GetChannelMessagesQuery(GetCurrentUserId(), channelId, search, take),
            cancellationToken);
        return Ok(ApiResponse.Ok(messages, "Messages retrieved successfully."));
    }

    [HttpPost("{channelId:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<ChatMessageDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ChatMessageDto>>> SendMessage(
        Guid channelId,
        SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var message = await mediator.Send(
            new SendChatMessageCommand(GetCurrentUserId(), channelId, request.Body),
            cancellationToken);
        return CreatedAtAction(
            nameof(GetMessages),
            new { channelId },
            ApiResponse.Ok(message, "Message sent successfully."));
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
