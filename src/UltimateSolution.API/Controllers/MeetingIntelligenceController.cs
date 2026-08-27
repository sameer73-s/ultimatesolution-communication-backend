using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UltimateSolution.API.Common.Models;
using UltimateSolution.Application.Features.AiSummary;
using UltimateSolution.Domain.Identity;

namespace UltimateSolution.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class MeetingIntelligenceController(IMediator mediator) : ControllerBase
{
    [HttpPost("recordings/{recordingId:guid}/transcription")]
    public async Task<ActionResult<ApiResponse<TranscriptionJobDto>>> RequestTranscription(Guid recordingId, CancellationToken cancellationToken) =>
        Accepted(ApiResponse.Ok(await mediator.Send(new RequestTranscriptionCommand(GetCurrentUserId(), IsManager(), recordingId), cancellationToken), "Transcription request accepted."));

    [HttpGet("meetings/{meetingId:guid}/transcription")]
    public async Task<ActionResult<ApiResponse<TranscriptionJobDto>>> GetTranscription(Guid meetingId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await mediator.Send(new GetMeetingTranscriptionQuery(GetCurrentUserId(), meetingId), cancellationToken), "Meeting transcription retrieved successfully."));

    [HttpPost("meetings/{meetingId:guid}/summary/generate")]
    public async Task<ActionResult<ApiResponse<MeetingSummaryDto>>> GenerateSummary(Guid meetingId, CancellationToken cancellationToken) =>
        Accepted(ApiResponse.Ok(await mediator.Send(new GenerateMeetingSummaryCommand(GetCurrentUserId(), IsManager(), meetingId), cancellationToken), "Meeting summary generation accepted."));

    [HttpGet("meetings/{meetingId:guid}/summary")]
    public async Task<ActionResult<ApiResponse<MeetingSummaryDto>>> GetSummary(Guid meetingId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await mediator.Send(new GetMeetingSummaryQuery(GetCurrentUserId(), meetingId), cancellationToken), "Meeting summary retrieved successfully."));

    [HttpPost("meetings/{meetingId:guid}/summary/approve")]
    public async Task<ActionResult<ApiResponse<MeetingSummaryDto>>> ApproveSummary(Guid meetingId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await mediator.Send(new ApproveMeetingSummaryCommand(GetCurrentUserId(), meetingId), cancellationToken), "Meeting summary approved successfully."));

    private bool IsManager() => User.IsInRole(SystemRoles.Manager) || User.IsInRole(SystemRoles.Admin);
    private Guid GetCurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : throw new UnauthorizedAccessException();
}
