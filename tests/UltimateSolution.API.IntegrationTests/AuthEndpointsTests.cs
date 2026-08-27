using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UltimateSolution.API.IntegrationTests;

public sealed class AuthEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task RegisterLoginRefreshAndProfileFollowTheUnifiedContract()
    {
        using var client = factory.CreateClient();
        var email = $"employee.{Guid.NewGuid():N}@ultimatesolution.test";
        const string password = "StrongPassword!2026";

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password, displayName = "Test Employee" });
        using var registerBody = JsonDocument.Parse(await registerResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.True(registerBody.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("Employee", registerBody.RootElement.GetProperty("data").GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
        var refreshToken = registerBody.RootElement.GetProperty("data").GetProperty("refreshToken").GetString();

        using var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        using var loginBody = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.True(loginBody.RootElement.GetProperty("success").GetBoolean());
        var accessToken = loginBody.RootElement.GetProperty("data").GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        using var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        using var refreshBody = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.True(refreshBody.RootElement.GetProperty("success").GetBoolean());
        Assert.NotEqual(refreshToken, refreshBody.RootElement.GetProperty("data").GetProperty("refreshToken").GetString());

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        using var profileResponse = await client.GetAsync("/api/v1/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        using var profileBody = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
        Assert.True(profileBody.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(email, profileBody.RootElement.GetProperty("data").GetProperty("email").GetString());
    }

    [Fact]
    public async Task RegisterReturnsValidationEnvelopeForAnInvalidPassword()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email = "invalid@ultimatesolution.test", password = "weak", displayName = "Invalid User" });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.NotEqual(0, body.RootElement.GetProperty("errors").GetArrayLength());
    }
}
