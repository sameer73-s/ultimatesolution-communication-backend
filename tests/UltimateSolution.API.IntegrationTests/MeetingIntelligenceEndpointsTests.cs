using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using UltimateSolution.Domain.Identity;
using UltimateSolution.Infrastructure.Identity;

namespace UltimateSolution.API.IntegrationTests;

public sealed class MeetingIntelligenceEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task MeetingIntelligenceUsesTestAdapterDraftApprovalPolicyAndConfirmedActionItems()
    {
        using var organizerClient = factory.CreateClient();
        using var attendeeClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();
        var organizer = await RegisterAndAuthenticateAsync(organizerClient, "ai-organizer");
        var attendee = await RegisterAndAuthenticateAsync(attendeeClient, "ai-attendee");
        var manager = await RegisterAndAuthenticateAsync(managerClient, "ai-manager");
        await GrantManagerRoleAsync(manager.UserId);
        var scheduledStartUtc = DateTimeOffset.UtcNow.AddHours(1);
        var scheduledEndUtc = scheduledStartUtc.AddMinutes(30);

        using var scheduleResponse = await organizerClient.PostAsJsonAsync(
            "/api/v1/meetings",
            new
            {
                title = "AI meeting intelligence review",
                agenda = "Review the planned release.",
                scheduledStartUtc,
                scheduledEndUtc,
                participantUserIds = new[] { attendee.UserId }
            });
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);
        var meetingId = GetData(scheduleResponse).GetProperty("id").GetGuid();

        using var startResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        using var startRecordingResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/recording/start", null);
        Assert.Equal(HttpStatusCode.OK, startRecordingResponse.StatusCode);
        var recordingId = GetData(startRecordingResponse).GetProperty("id").GetGuid();
        using var stopRecordingResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/recording/stop", null);
        Assert.Equal(HttpStatusCode.OK, stopRecordingResponse.StatusCode);

        using var unauthorizedTranscriptionResponse = await attendeeClient.PostAsync($"/api/v1/recordings/{recordingId}/transcription", null);
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedTranscriptionResponse.StatusCode);

        using var requestTranscriptionResponse = await organizerClient.PostAsync($"/api/v1/recordings/{recordingId}/transcription", null);
        Assert.Equal(HttpStatusCode.Accepted, requestTranscriptionResponse.StatusCode);
        using var transcriptionBody = JsonDocument.Parse(await requestTranscriptionResponse.Content.ReadAsStringAsync());
        Assert.True(transcriptionBody.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(3, transcriptionBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());
        Assert.Equal(2, transcriptionBody.RootElement.GetProperty("data").GetProperty("segments").GetArrayLength());

        using var getTranscriptionResponse = await attendeeClient.GetAsync($"/api/v1/meetings/{meetingId}/transcription");
        Assert.Equal(HttpStatusCode.OK, getTranscriptionResponse.StatusCode);

        using var unauthorizedGenerationResponse = await attendeeClient.PostAsync($"/api/v1/meetings/{meetingId}/summary/generate", null);
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedGenerationResponse.StatusCode);

        using var generateSummaryResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/summary/generate", null);
        Assert.Equal(HttpStatusCode.Accepted, generateSummaryResponse.StatusCode);
        using var summaryBody = JsonDocument.Parse(await generateSummaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, summaryBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());
        Assert.Equal(2, summaryBody.RootElement.GetProperty("data").GetProperty("decisions").GetArrayLength());
        Assert.Equal(1, summaryBody.RootElement.GetProperty("data").GetProperty("proposedActionItems").GetArrayLength());

        using var getSummaryResponse = await attendeeClient.GetAsync($"/api/v1/meetings/{meetingId}/summary");
        Assert.Equal(HttpStatusCode.OK, getSummaryResponse.StatusCode);
        using var getSummaryBody = JsonDocument.Parse(await getSummaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, getSummaryBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());

        using var unauthorizedApprovalResponse = await attendeeClient.PostAsync($"/api/v1/meetings/{meetingId}/summary/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedApprovalResponse.StatusCode);

        using var managerApprovalResponse = await managerClient.PostAsync($"/api/v1/meetings/{meetingId}/summary/approve", null);
        Assert.Equal(HttpStatusCode.OK, managerApprovalResponse.StatusCode);
        using var approvedSummaryBody = JsonDocument.Parse(await managerApprovalResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, approvedSummaryBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());
        Assert.Equal(manager.UserId, approvedSummaryBody.RootElement.GetProperty("data").GetProperty("approvedByUserId").GetGuid());

        using var actionItemsResponse = await attendeeClient.GetAsync("/api/v1/action-items");
        Assert.Equal(HttpStatusCode.OK, actionItemsResponse.StatusCode);
        using var actionItemsBody = JsonDocument.Parse(await actionItemsResponse.Content.ReadAsStringAsync());
        Assert.Single(actionItemsBody.RootElement.GetProperty("data").EnumerateArray());
        var actionItem = actionItemsBody.RootElement.GetProperty("data")[0];
        var actionItemId = actionItem.GetProperty("id").GetGuid();
        Assert.Equal(1, actionItem.GetProperty("status").GetInt32());

        using var updateActionItemResponse = await attendeeClient.PatchAsJsonAsync(
            $"/api/v1/action-items/{actionItemId}",
            new
            {
                title = "Prepare the release plan",
                description = "Prepared for internal review.",
                assigneeUserId = attendee.UserId,
                dueAtUtc = (DateTimeOffset?)null,
                status = 2
            });
        Assert.Equal(HttpStatusCode.OK, updateActionItemResponse.StatusCode);
        using var updatedActionItemBody = JsonDocument.Parse(await updateActionItemResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, updatedActionItemBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());

        using var secondApprovalResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/summary/approve", null);
        Assert.Equal(HttpStatusCode.BadRequest, secondApprovalResponse.StatusCode);
    }

    private async Task GrantManagerRoleAsync(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        Assert.NotNull(user);
        var roleResult = await userManager.AddToRoleAsync(user, SystemRoles.Manager);
        Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(error => error.Description)));
    }

    private static JsonElement GetData(HttpResponseMessage response)
    {
        var document = JsonDocument.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        return document.RootElement.GetProperty("data").Clone();
    }

    private static async Task<AuthenticatedTestUser> RegisterAndAuthenticateAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}.{Guid.NewGuid():N}@ultimatesolution.test";
        const string password = "StrongPassword!2026";
        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password, displayName = $"{prefix} user" });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        using var registerBody = JsonDocument.Parse(await registerResponse.Content.ReadAsStringAsync());
        var accessToken = registerBody.RootElement.GetProperty("data").GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var profileResponse = await client.GetAsync("/api/v1/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        using var profileBody = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
        return new AuthenticatedTestUser(profileBody.RootElement.GetProperty("data").GetProperty("userId").GetGuid(), accessToken);
    }

    private sealed record AuthenticatedTestUser(Guid UserId, string AccessToken);
}
