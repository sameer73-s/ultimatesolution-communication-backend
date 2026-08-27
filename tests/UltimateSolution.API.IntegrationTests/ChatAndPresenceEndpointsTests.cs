using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace UltimateSolution.API.IntegrationTests;

public sealed class ChatAndPresenceEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ChannelMessageSearchReadArchiveAndPresenceFollowTheUnifiedContract()
    {
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();
        var owner = await RegisterAndAuthenticateAsync(ownerClient, "owner");
        var member = await RegisterAndAuthenticateAsync(memberClient, "member");

        using var invalidMemberResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/channels",
            new { type = 2, name = "Invalid membership", memberIds = new[] { Guid.NewGuid() } });
        Assert.Equal(HttpStatusCode.BadRequest, invalidMemberResponse.StatusCode);
        using var invalidMemberBody = JsonDocument.Parse(await invalidMemberResponse.Content.ReadAsStringAsync());
        Assert.False(invalidMemberBody.RootElement.GetProperty("success").GetBoolean());

        using var createChannelResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/channels",
            new
            {
                type = 2,
                name = "Release Planning",
                memberIds = new[] { member.UserId }
            });
        Assert.Equal(HttpStatusCode.Created, createChannelResponse.StatusCode);
        using var createChannelBody = JsonDocument.Parse(await createChannelResponse.Content.ReadAsStringAsync());
        Assert.True(createChannelBody.RootElement.GetProperty("success").GetBoolean());
        var channelId = createChannelBody.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        using var memberChannelsResponse = await memberClient.GetAsync("/api/v1/channels");
        Assert.Equal(HttpStatusCode.OK, memberChannelsResponse.StatusCode);
        using var memberChannelsBody = JsonDocument.Parse(await memberChannelsResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            memberChannelsBody.RootElement.GetProperty("data").EnumerateArray(),
            channel => channel.GetProperty("id").GetGuid() == channelId);

        using var sendMessageResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/channels/{channelId}/messages",
            new { body = "Release candidate is ready for review." });
        Assert.Equal(HttpStatusCode.Created, sendMessageResponse.StatusCode);
        using var sendMessageBody = JsonDocument.Parse(await sendMessageResponse.Content.ReadAsStringAsync());
        var messageId = sendMessageBody.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        using var updateMessageResponse = await ownerClient.PatchAsJsonAsync(
            $"/api/v1/messages/{messageId}",
            new { body = "Release candidate is approved for deployment." });
        Assert.Equal(HttpStatusCode.OK, updateMessageResponse.StatusCode);
        using var updateMessageBody = JsonDocument.Parse(await updateMessageResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "Release candidate is approved for deployment.",
            updateMessageBody.RootElement.GetProperty("data").GetProperty("body").GetString());
        Assert.Equal(JsonValueKind.String, updateMessageBody.RootElement.GetProperty("data").GetProperty("editedAtUtc").ValueKind);

        using var searchResponse = await memberClient.GetAsync(
            $"/api/v1/channels/{channelId}/messages?search=approved&take=10");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        using var searchBody = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
        Assert.Single(searchBody.RootElement.GetProperty("data").EnumerateArray());
        Assert.Equal(messageId, searchBody.RootElement.GetProperty("data")[0].GetProperty("id").GetGuid());

        using var markReadResponse = await memberClient.PostAsync($"/api/v1/messages/{messageId}/read", null);
        Assert.Equal(HttpStatusCode.OK, markReadResponse.StatusCode);
        using var markReadBody = JsonDocument.Parse(await markReadResponse.Content.ReadAsStringAsync());
        Assert.Equal(messageId, markReadBody.RootElement.GetProperty("data").GetProperty("lastReadMessageId").GetGuid());

        using var presenceResponse = await ownerClient.GetAsync($"/api/v1/presence/{member.UserId}");
        Assert.Equal(HttpStatusCode.OK, presenceResponse.StatusCode);
        using var presenceBody = JsonDocument.Parse(await presenceResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, presenceBody.RootElement.GetProperty("data").GetProperty("status").GetInt32());

        using var archiveResponse = await ownerClient.PatchAsJsonAsync(
            $"/api/v1/channels/{channelId}",
            new { isArchived = true });
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        using var archiveBody = JsonDocument.Parse(await archiveResponse.Content.ReadAsStringAsync());
        Assert.True(archiveBody.RootElement.GetProperty("data").GetProperty("isArchived").GetBoolean());

        using var archivedMessageResponse = await memberClient.PostAsJsonAsync(
            $"/api/v1/channels/{channelId}/messages",
            new { body = "This message must be rejected." });
        Assert.Equal(HttpStatusCode.BadRequest, archivedMessageResponse.StatusCode);
        using var archivedMessageBody = JsonDocument.Parse(await archivedMessageResponse.Content.ReadAsStringAsync());
        Assert.False(archivedMessageBody.RootElement.GetProperty("success").GetBoolean());

        using var deleteMessageResponse = await ownerClient.DeleteAsync($"/api/v1/messages/{messageId}");
        Assert.Equal(HttpStatusCode.OK, deleteMessageResponse.StatusCode);
        using var deleteMessageBody = JsonDocument.Parse(await deleteMessageResponse.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.String, deleteMessageBody.RootElement.GetProperty("data").GetProperty("deletedAtUtc").ValueKind);

        using var messagesAfterDeleteResponse = await ownerClient.GetAsync($"/api/v1/channels/{channelId}/messages?take=10");
        Assert.Equal(HttpStatusCode.OK, messagesAfterDeleteResponse.StatusCode);
        using var messagesAfterDeleteBody = JsonDocument.Parse(await messagesAfterDeleteResponse.Content.ReadAsStringAsync());
        Assert.Empty(messagesAfterDeleteBody.RootElement.GetProperty("data").EnumerateArray());
    }

    [Fact]
    public async Task ChatHubNegotiationRequiresAuthenticationAndAcceptsBearerToken()
    {
        using var anonymousClient = factory.CreateClient();
        using var anonymousResponse = await anonymousClient.PostAsync("/hubs/chat/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        using var anonymousBody = JsonDocument.Parse(await anonymousResponse.Content.ReadAsStringAsync());
        Assert.False(anonymousBody.RootElement.GetProperty("success").GetBoolean());

        using var authenticatedClient = factory.CreateClient();
        _ = await RegisterAndAuthenticateAsync(authenticatedClient, "hub");
        using var negotiateResponse = await authenticatedClient.PostAsync("/hubs/chat/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.OK, negotiateResponse.StatusCode);
        using var negotiateBody = JsonDocument.Parse(await negotiateResponse.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(negotiateBody.RootElement.GetProperty("connectionToken").GetString()));
    }

    [Fact]
    public async Task ChatHubPublishesPresenceTypingAndPersistedMessageEvents()
    {
        using var ownerClient = factory.CreateClient();
        using var memberClient = factory.CreateClient();
        var owner = await RegisterAndAuthenticateAsync(ownerClient, "realtime-owner");
        var member = await RegisterAndAuthenticateAsync(memberClient, "realtime-member");

        using var createChannelResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/channels",
            new { type = 2, name = "Realtime channel", memberIds = new[] { member.UserId } });
        Assert.Equal(HttpStatusCode.Created, createChannelResponse.StatusCode);
        using var createChannelBody = JsonDocument.Parse(await createChannelResponse.Content.ReadAsStringAsync());
        var channelId = createChannelBody.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        await using var ownerHub = CreateHubConnection(owner.AccessToken);
        await using var memberHub = CreateHubConnection(member.AccessToken);
        var memberOnline = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var memberAway = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var memberOffline = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var typingChanged = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var messageCreated = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        ownerHub.On<JsonElement>("presenceChanged", presence =>
        {
            if (presence.GetProperty("userId").GetGuid() != member.UserId)
            {
                return;
            }

            switch (presence.GetProperty("status").GetInt32())
            {
                case 2:
                    memberOnline.TrySetResult(presence);
                    break;
                case 3:
                    memberAway.TrySetResult(presence);
                    break;
                case 1:
                    memberOffline.TrySetResult(presence);
                    break;
            }
        });
        ownerHub.On<JsonElement>("typingChanged", typing => typingChanged.TrySetResult(typing));
        memberHub.On<JsonElement>("messageCreated", message => messageCreated.TrySetResult(message));

        await ownerHub.StartAsync();
        await memberHub.StartAsync();
        await ownerHub.InvokeAsync("SubscribeChannel", channelId);
        await memberHub.InvokeAsync("SubscribeChannel", channelId);

        var onlinePresence = await memberOnline.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(2, onlinePresence.GetProperty("status").GetInt32());

        await memberHub.InvokeAsync("SetPresence", 3);
        var awayPresence = await memberAway.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(3, awayPresence.GetProperty("status").GetInt32());

        await memberHub.InvokeAsync("StartTyping", channelId);
        var typingEvent = await typingChanged.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(typingEvent.GetProperty("isTyping").GetBoolean());
        Assert.Equal(member.UserId, typingEvent.GetProperty("userId").GetGuid());

        using var sendMessageResponse = await ownerClient.PostAsJsonAsync(
            $"/api/v1/channels/{channelId}/messages",
            new { body = "This persisted message is also broadcast." });
        Assert.Equal(HttpStatusCode.Created, sendMessageResponse.StatusCode);
        using var sendMessageBody = JsonDocument.Parse(await sendMessageResponse.Content.ReadAsStringAsync());
        var messageId = sendMessageBody.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var broadcastMessage = await messageCreated.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(messageId, broadcastMessage.GetProperty("id").GetGuid());
        Assert.Equal(channelId, broadcastMessage.GetProperty("channelId").GetGuid());

        await memberHub.StopAsync();
        var offlinePresence = await memberOffline.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, offlinePresence.GetProperty("status").GetInt32());
    }

    private HubConnection CreateHubConnection(string accessToken) =>
        new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chat", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

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
        var userId = profileBody.RootElement.GetProperty("data").GetProperty("userId").GetGuid();

        return new AuthenticatedTestUser(userId, accessToken);
    }

    private sealed record AuthenticatedTestUser(Guid UserId, string AccessToken);
}
