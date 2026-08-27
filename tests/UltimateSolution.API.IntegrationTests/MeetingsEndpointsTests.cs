using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace UltimateSolution.API.IntegrationTests;

public sealed class MeetingsEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task MeetingLifecycleUsesAuthorizedProviderNeutralMediaContracts()
    {
        using var organizerClient = factory.CreateClient();
        using var attendeeClient = factory.CreateClient();
        var organizer = await RegisterAndAuthenticateAsync(organizerClient, "meeting-organizer");
        var attendee = await RegisterAndAuthenticateAsync(attendeeClient, "meeting-attendee");
        var scheduledStartUtc = DateTimeOffset.UtcNow.AddHours(1);
        var scheduledEndUtc = scheduledStartUtc.AddMinutes(45);

        using var scheduleResponse = await organizerClient.PostAsJsonAsync(
            "/api/v1/meetings",
            new
            {
                title = "Quarterly release planning",
                agenda = "Confirm scope and release responsibilities.",
                scheduledStartUtc,
                scheduledEndUtc,
                participantUserIds = new[] { attendee.UserId }
            });
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);
        using var scheduleBody = JsonDocument.Parse(await scheduleResponse.Content.ReadAsStringAsync());
        var meetingId = scheduleBody.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        Assert.Equal(1, scheduleBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());
        Assert.Equal(2, scheduleBody.RootElement.GetProperty("data").GetProperty("participants").GetArrayLength());

        using var unauthorizedUpdateResponse = await attendeeClient.PatchAsJsonAsync(
            $"/api/v1/meetings/{meetingId}",
            new
            {
                title = "Unauthorized amendment",
                agenda = "Must not be applied.",
                scheduledStartUtc,
                scheduledEndUtc
            });
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedUpdateResponse.StatusCode);
        using var unauthorizedUpdateBody = JsonDocument.Parse(await unauthorizedUpdateResponse.Content.ReadAsStringAsync());
        Assert.False(unauthorizedUpdateBody.RootElement.GetProperty("success").GetBoolean());

        using var startResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        using var startBody = JsonDocument.Parse(await startResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, startBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(startBody.RootElement.GetProperty("data").GetProperty("mediaSessionReference").GetString()));

        using var joinResponse = await attendeeClient.PostAsync($"/api/v1/meetings/{meetingId}/join", null);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
        using var joinBody = JsonDocument.Parse(await joinResponse.Content.ReadAsStringAsync());
        var mediaSessionReference = joinBody.RootElement.GetProperty("data").GetProperty("mediaSessionReference").GetString();
        var mediaJoinUrl = joinBody.RootElement.GetProperty("data").GetProperty("mediaJoinUrl").GetString();
        Assert.False(string.IsNullOrWhiteSpace(mediaSessionReference));
        Assert.True(Uri.TryCreate(mediaJoinUrl, UriKind.Absolute, out _));

        using var startRecordingResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/recording/start", null);
        var startRecordingContent = await startRecordingResponse.Content.ReadAsStringAsync();
        Assert.True(startRecordingResponse.StatusCode == HttpStatusCode.OK, startRecordingContent);
        using var startRecordingBody = JsonDocument.Parse(startRecordingContent);
        Assert.Equal(1, startRecordingBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());

        using var stopRecordingResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/recording/stop", null);
        Assert.Equal(HttpStatusCode.OK, stopRecordingResponse.StatusCode);
        using var stopRecordingBody = JsonDocument.Parse(await stopRecordingResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, stopRecordingBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());

        using var recordingsResponse = await attendeeClient.GetAsync($"/api/v1/meetings/{meetingId}/recordings");
        Assert.Equal(HttpStatusCode.OK, recordingsResponse.StatusCode);
        using var recordingsBody = JsonDocument.Parse(await recordingsResponse.Content.ReadAsStringAsync());
        Assert.Single(recordingsBody.RootElement.GetProperty("data").EnumerateArray());

        using var leaveResponse = await attendeeClient.PostAsync($"/api/v1/meetings/{meetingId}/leave", null);
        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);

        using var endResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/end", null);
        Assert.Equal(HttpStatusCode.OK, endResponse.StatusCode);
        using var endBody = JsonDocument.Parse(await endResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, endBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());

        using var postCompletionUpdateResponse = await organizerClient.PatchAsJsonAsync(
            $"/api/v1/meetings/{meetingId}",
            new
            {
                title = "Post-completion amendment",
                agenda = "Must be rejected.",
                scheduledStartUtc,
                scheduledEndUtc
            });
        Assert.Equal(HttpStatusCode.BadRequest, postCompletionUpdateResponse.StatusCode);
        using var postCompletionUpdateBody = JsonDocument.Parse(await postCompletionUpdateResponse.Content.ReadAsStringAsync());
        Assert.False(postCompletionUpdateBody.RootElement.GetProperty("success").GetBoolean());
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
        return new AuthenticatedTestUser(
            profileBody.RootElement.GetProperty("data").GetProperty("userId").GetGuid(),
            accessToken);
    }

    private sealed record AuthenticatedTestUser(Guid UserId, string AccessToken);
}
