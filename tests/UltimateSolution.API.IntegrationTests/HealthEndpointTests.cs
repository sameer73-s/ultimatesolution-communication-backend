using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UltimateSolution.API.IntegrationTests;

public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetHealthReturnsStandardSuccessEnvelope()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/health");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("API is running.", body.RootElement.GetProperty("message").GetString());
        Assert.Equal("Healthy", body.RootElement.GetProperty("data").GetProperty("status").GetString());
        Assert.Equal(0, body.RootElement.GetProperty("errors").GetArrayLength());
    }
}
