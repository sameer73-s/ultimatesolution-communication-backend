using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using UltimateSolution.Application.Common.Results;
using UltimateSolution.Application.Features.AiSummary;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Identity;
using UltimateSolution.Infrastructure.Identity;

namespace UltimateSolution.Infrastructure.ExternalServices.Ai;

public sealed class ExternalMeetingIntelligenceOptions
{
    public const string SectionName = "MeetingIntelligence";

    public string ServiceUrl { get; init; } = string.Empty;
    public string? ApiKey { get; init; }
}

public sealed class ExternalMeetingIntelligenceService(
    HttpClient httpClient,
    IOptions<ExternalMeetingIntelligenceOptions> options) : ITranscriptionService, ISummaryService
{
    public async Task<Result<TranscriptionSubmissionResult>> SubmitAsync(TranscriptionSubmissionRequest request, CancellationToken cancellationToken)
    {
        if (!TryConfigureClient())
        {
            return ResultFactory.Failure<TranscriptionSubmissionResult>("meeting_intelligence_service_not_configured");
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "v1/transcriptions",
                new ExternalTranscriptionRequest(request.MeetingId, request.RecordingId, request.MediaRecordingReference),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ResultFactory.Failure<TranscriptionSubmissionResult>("external_transcription_submission_failed");
            }

            var content = await response.Content.ReadFromJsonAsync<ExternalTranscriptionResponse>(cancellationToken);
            if (content is null || string.IsNullOrWhiteSpace(content.ExternalJobReference))
            {
                return ResultFactory.Failure<TranscriptionSubmissionResult>("external_transcription_response_invalid");
            }

            return ResultFactory.Success(new TranscriptionSubmissionResult(content.ExternalJobReference, content.Segments ?? Array.Empty<TranscriptionSegmentDto>(), content.IsCompleted));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return ResultFactory.Failure<TranscriptionSubmissionResult>("external_transcription_service_unavailable");
        }
        catch (JsonException)
        {
            return ResultFactory.Failure<TranscriptionSubmissionResult>("external_transcription_response_invalid");
        }
        catch (NotSupportedException)
        {
            return ResultFactory.Failure<TranscriptionSubmissionResult>("external_transcription_response_invalid");
        }
    }

    public async Task<Result<GeneratedMeetingSummary>> GenerateAsync(GenerateMeetingSummaryRequest request, CancellationToken cancellationToken)
    {
        if (!TryConfigureClient())
        {
            return ResultFactory.Failure<GeneratedMeetingSummary>("meeting_intelligence_service_not_configured");
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "v1/summaries",
                new ExternalSummaryRequest(request.MeetingId, request.TranscriptionJobId, request.Transcript, request.Participants),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ResultFactory.Failure<GeneratedMeetingSummary>("external_summary_generation_failed");
            }

            var content = await response.Content.ReadFromJsonAsync<ExternalSummaryResponse>(cancellationToken);
            if (content is null || string.IsNullOrWhiteSpace(content.Content))
            {
                return ResultFactory.Failure<GeneratedMeetingSummary>("external_summary_response_invalid");
            }

            return ResultFactory.Success(new GeneratedMeetingSummary(
                content.Content,
                content.Decisions ?? Array.Empty<string>(),
                content.ProposedActionItems ?? Array.Empty<ProposedActionItemDto>(),
                content.ExternalSummaryReference));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return ResultFactory.Failure<GeneratedMeetingSummary>("external_summary_service_unavailable");
        }
        catch (JsonException)
        {
            return ResultFactory.Failure<GeneratedMeetingSummary>("external_summary_response_invalid");
        }
        catch (NotSupportedException)
        {
            return ResultFactory.Failure<GeneratedMeetingSummary>("external_summary_response_invalid");
        }
    }

    private bool TryConfigureClient()
    {
        var configuredOptions = options.Value;
        if (!Uri.TryCreate(configuredOptions.ServiceUrl, UriKind.Absolute, out var baseAddress))
        {
            return false;
        }

        httpClient.BaseAddress = new Uri(baseAddress.ToString().TrimEnd('/') + "/");
        if (!string.IsNullOrWhiteSpace(configuredOptions.ApiKey))
        {
            httpClient.DefaultRequestHeaders.Remove("X-Meeting-Intelligence-Key");
            httpClient.DefaultRequestHeaders.Add("X-Meeting-Intelligence-Key", configuredOptions.ApiKey);
        }

        return true;
    }

    private sealed record ExternalTranscriptionRequest(Guid MeetingId, Guid RecordingId, string MediaRecordingReference);
    private sealed record ExternalTranscriptionResponse(string ExternalJobReference, bool IsCompleted, IReadOnlyCollection<TranscriptionSegmentDto>? Segments);
    private sealed record ExternalSummaryRequest(Guid MeetingId, Guid TranscriptionJobId, string Transcript, IReadOnlyCollection<MeetingSummaryParticipant> Participants);
    private sealed record ExternalSummaryResponse(string Content, IReadOnlyCollection<string>? Decisions, IReadOnlyCollection<ProposedActionItemDto>? ProposedActionItems, string? ExternalSummaryReference);
}

public sealed class TestMeetingIntelligenceService : ITranscriptionService, ISummaryService
{
    private static readonly string[] TestDecisions = ["Prepare the release plan.", "Confirm the delivery date."];

    public Task<Result<TranscriptionSubmissionResult>> SubmitAsync(TranscriptionSubmissionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var segments = new[]
        {
            new TranscriptionSegmentDto(1, "The team agreed to prepare the release plan.", "Speaker 1", TimeSpan.Zero, TimeSpan.FromSeconds(8)),
            new TranscriptionSegmentDto(2, "The organizer will confirm the delivery date.", "Speaker 2", TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16))
        };
        return Task.FromResult(ResultFactory.Success(new TranscriptionSubmissionResult($"test-transcription-{request.RecordingId:N}", segments, true)));
    }

    public Task<Result<GeneratedMeetingSummary>> GenerateAsync(GenerateMeetingSummaryRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var assigneeUserId = request.Participants.FirstOrDefault()?.UserId;
        var proposedActionItems = new[]
        {
            new ProposedActionItemDto("Prepare the release plan", "Prepare and circulate the agreed release plan.", assigneeUserId == Guid.Empty ? null : assigneeUserId, null)
        };
        return Task.FromResult(ResultFactory.Success(new GeneratedMeetingSummary(
            "The team agreed to prepare the release plan and confirm the delivery date.",
            TestDecisions,
            proposedActionItems,
            $"test-summary-{request.TranscriptionJobId:N}")));
    }
}

public sealed class OrganizerOrManagerMeetingSummaryApprovalPolicy(UserManager<ApplicationUser> userManager) : IMeetingSummaryApprovalPolicy
{
    public async Task<Result> AuthorizeAsync(MeetingSummaryApprovalAuthorizationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.RequestingUserId == request.OrganizerUserId)
        {
            return Result.Success();
        }

        var user = await userManager.FindByIdAsync(request.RequestingUserId.ToString());
        if (user is null)
        {
            return Result.Failure("meeting_summary_approval_not_authorized");
        }

        var roles = await userManager.GetRolesAsync(user);
        return roles.Contains(SystemRoles.Manager, StringComparer.Ordinal) || roles.Contains(SystemRoles.Admin, StringComparer.Ordinal)
            ? Result.Success()
            : Result.Failure("meeting_summary_approval_not_authorized");
    }
}
