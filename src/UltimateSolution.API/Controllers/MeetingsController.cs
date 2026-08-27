using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;
using UltimateSolution.API.Contracts.Meetings;
using UltimateSolution.Application.Features.Meetings;
using UltimateSolution.Domain.Identity;

namespace UltimateSolution.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/meetings")]
public sealed class MeetingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MeetingDto>>>> GetMeetings(CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new GetMeetingsQuery(GetCurrentUserId()), cancellationToken), "Meetings retrieved successfully."));
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MeetingDto>>> Schedule(ScheduleMeetingRequest request, CancellationToken cancellationToken)
    {
        var meeting = await mediator.Send(new ScheduleMeetingCommand(GetCurrentUserId(), request.Title, request.Agenda, request.ScheduledStartUtc, request.ScheduledEndUtc, request.ParticipantUserIds), cancellationToken);
        return CreatedAtAction(nameof(GetMeeting), new { meetingId = meeting.Id }, ApiResponse.Ok(meeting, "Meeting scheduled successfully."));
    }
    [HttpGet("{meetingId:guid}")]
    public async Task<ActionResult<ApiResponse<MeetingDto>>> GetMeeting(Guid meetingId, CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new GetMeetingQuery(GetCurrentUserId(), meetingId), cancellationToken), "Meeting retrieved successfully."));
    [HttpPatch("{meetingId:guid}")]
    public async Task<ActionResult<ApiResponse<MeetingDto>>> Update(Guid meetingId, UpdateMeetingRequest request, CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new UpdateMeetingCommand(GetCurrentUserId(), IsManager(), meetingId, request.Title, request.Agenda, request.ScheduledStartUtc, request.ScheduledEndUtc), cancellationToken), "Meeting updated successfully."));
    [HttpPost("{meetingId:guid}/participants")]
    public async Task<ActionResult<ApiResponse<MeetingDto>>> Invite(Guid meetingId, InviteMeetingParticipantRequest request, CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new InviteMeetingParticipantCommand(GetCurrentUserId(), IsManager(), meetingId, request.UserId), cancellationToken), "Meeting participant invited successfully."));
    [HttpDelete("{meetingId:guid}/participants/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<MeetingDto>>> RemoveParticipant(Guid meetingId, Guid userId, CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new RemoveMeetingParticipantCommand(GetCurrentUserId(), IsManager(), meetingId, userId), cancellationToken), "Meeting participant removed successfully."));
    [HttpPost("{meetingId:guid}/start")]
    public async Task<ActionResult<ApiResponse<MeetingDto>>> Start(Guid meetingId, CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new StartMeetingCommand(GetCurrentUserId(), IsManager(), meetingId), cancellationToken), "Meeting started successfully."));
    [HttpPost("{meetingId:guid}/end")]
    public async Task<ActionResult<ApiResponse<MeetingDto>>> End(Guid meetingId, CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new EndMeetingCommand(GetCurrentUserId(), IsManager(), meetingId), cancellationToken), "Meeting ended successfully."));
    [HttpPost("{meetingId:guid}/join")]
    public async Task<ActionResult<ApiResponse<JoinMeetingResult>>> Join(Guid meetingId, CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new JoinMeetingCommand(GetCurrentUserId(), meetingId), cancellationToken), "Meeting join session created."));
    [HttpPost("{meetingId:guid}/leave")]
    public async Task<ActionResult<ApiResponse<object>>> Leave(Guid meetingId, CancellationToken cancellationToken) { await mediator.Send(new LeaveMeetingCommand(GetCurrentUserId(), meetingId), cancellationToken); return Ok(ApiResponse.Ok<object>(new { }, "Meeting left successfully.")); }
    [HttpPost("{meetingId:guid}/recording/start")]
    public async Task<ActionResult<ApiResponse<MeetingRecordingDto>>> StartRecording(Guid meetingId, CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new StartMeetingRecordingCommand(GetCurrentUserId(), IsManager(), meetingId), cancellationToken), "Meeting recording started successfully."));
    [HttpPost("{meetingId:guid}/recording/stop")]
    public async Task<ActionResult<ApiResponse<MeetingRecordingDto>>> StopRecording(Guid meetingId, CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new StopMeetingRecordingCommand(GetCurrentUserId(), IsManager(), meetingId), cancellationToken), "Meeting recording stopped successfully."));
    [HttpGet("{meetingId:guid}/recordings")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MeetingRecordingDto>>>> GetRecordings(Guid meetingId, CancellationToken cancellationToken) => Ok(ApiResponse.Ok(await mediator.Send(new GetMeetingRecordingsQuery(GetCurrentUserId(), meetingId), cancellationToken), "Meeting recordings retrieved successfully."));
    private bool IsManager() => User.IsInRole(SystemRoles.Manager) || User.IsInRole(SystemRoles.Admin);
    private Guid GetCurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : throw new UnauthorizedAccessException();
}
