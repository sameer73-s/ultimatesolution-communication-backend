using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace UltimateSolution.API.IntegrationTests;

public sealed class NotificationsEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task NotificationsPersistPublishToTheRecipientAndMarkReadThroughTheUnifiedContract()
    {
        using var organizerClient = factory.CreateClient();
        using var attendeeClient = factory.CreateClient();
        var organizer = await RegisterAndAuthenticateAsync(organizerClient, "notification-organizer");
        var attendee = await RegisterAndAuthenticateAsync(attendeeClient, "notification-attendee");
        var scheduledStartUtc = DateTimeOffset.UtcNow.AddHours(1);
        var scheduledEndUtc = scheduledStartUtc.AddMinutes(30);

        using var scheduleResponse = await organizerClient.PostAsJsonAsync(
            "/api/v1/meetings",
            new
            {
                title = "Notification workflow review",
                agenda = "Review the notification lifecycle.",
                scheduledStartUtc,
                scheduledEndUtc,
                participantUserIds = new[] { attendee.UserId }
            });
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);
        var meetingId = await GetDataIdAsync(scheduleResponse, "id");

        using var startResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        using var startRecordingResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/recording/start", null);
        Assert.Equal(HttpStatusCode.OK, startRecordingResponse.StatusCode);
        var recordingId = await GetDataIdAsync(startRecordingResponse, "id");
        using var stopRecordingResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/recording/stop", null);
        Assert.Equal(HttpStatusCode.OK, stopRecordingResponse.StatusCode);

        await using var notificationsHub = CreateNotificationsHubConnection(attendee.AccessToken);
        var summaryReady = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actionItemsCreated = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var notificationRead = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        notificationsHub.On<JsonElement>("notificationCreated", notification =>
        {
            if (notification.GetProperty("sourceType").GetString() == "MeetingSummary")
            {
                summaryReady.TrySetResult(notification);
            }
        });
        notificationsHub.On<JsonElement>("actionItemsCreated", actionItems => actionItemsCreated.TrySetResult(actionItems));
        notificationsHub.On<JsonElement>("notificationRead", notification => notificationRead.TrySetResult(notification));
        await notificationsHub.StartAsync();
        await notificationsHub.InvokeAsync("SubscribeUserNotifications");

        using var transcriptionResponse = await organizerClient.PostAsync($"/api/v1/recordings/{recordingId}/transcription", null);
        Assert.Equal(HttpStatusCode.Accepted, transcriptionResponse.StatusCode);
        using var generateSummaryResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/summary/generate", null);
        Assert.Equal(HttpStatusCode.Accepted, generateSummaryResponse.StatusCode);
        var summaryId = await GetDataIdAsync(generateSummaryResponse, "id");

        var summaryNotification = await summaryReady.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(summaryId, summaryNotification.GetProperty("sourceId").GetGuid());
        var summaryNotificationId = summaryNotification.GetProperty("id").GetGuid();

        using var getNotificationsResponse = await attendeeClient.GetAsync("/api/v1/notifications");
        Assert.Equal(HttpStatusCode.OK, getNotificationsResponse.StatusCode);
        using var getNotificationsBody = JsonDocument.Parse(await getNotificationsResponse.Content.ReadAsStringAsync());
        Assert.Single(getNotificationsBody.RootElement.GetProperty("data").EnumerateArray());

        using var markReadResponse = await attendeeClient.PostAsync($"/api/v1/notifications/{summaryNotificationId}/read", null);
        Assert.Equal(HttpStatusCode.OK, markReadResponse.StatusCode);
        var readNotification = await notificationRead.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(summaryNotificationId, readNotification.GetProperty("id").GetGuid());
        Assert.NotEqual(JsonValueKind.Null, readNotification.GetProperty("readAtUtc").ValueKind);

        using var approveSummaryResponse = await organizerClient.PostAsync($"/api/v1/meetings/{meetingId}/summary/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveSummaryResponse.StatusCode);
        var createdActionItems = await actionItemsCreated.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Single(createdActionItems.EnumerateArray());

        using var finalNotificationsResponse = await attendeeClient.GetAsync("/api/v1/notifications");
        Assert.Equal(HttpStatusCode.OK, finalNotificationsResponse.StatusCode);
        using var finalNotificationsBody = JsonDocument.Parse(await finalNotificationsResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, finalNotificationsBody.RootElement.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task NotificationsHubNegotiationRequiresAuthenticationAndAcceptsBearerToken()
    {
        using var anonymousClient = factory.CreateClient();
        using var anonymousResponse = await anonymousClient.PostAsync("/hubs/notifications/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var authenticatedClient = factory.CreateClient();
        _ = await RegisterAndAuthenticateAsync(authenticatedClient, "notification-hub");
        using var negotiateResponse = await authenticatedClient.PostAsync("/hubs/notifications/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.OK, negotiateResponse.StatusCode);
        using var negotiateBody = JsonDocument.Parse(await negotiateResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(negotiateBody.RootElement.GetProperty("connectionToken").GetString()));
    }

    private HubConnection CreateNotificationsHubConnection(string accessToken) =>
        new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

    private static async Task<Guid> GetDataIdAsync(HttpResponseMessage response, string propertyName)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("data").GetProperty(propertyName).GetGuid();
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
