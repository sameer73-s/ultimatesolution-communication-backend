using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UltimateSolution.API.IntegrationTests;

public sealed class OpenApiSecurityTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task PublishedOpenApiDocumentsJwtBearerSecurity()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("Json Web Token", bearer.GetProperty("bearerFormat").GetString());

        Assert.False(root.GetProperty("paths").GetProperty("/api/v1/auth/login").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(root.GetProperty("paths").GetProperty("/api/v1/auth/register").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(root.GetProperty("paths").GetProperty("/api/v1/auth/refresh").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(root.GetProperty("paths").GetProperty("/api/v1/health").GetProperty("get").TryGetProperty("security", out _));
        Assert.True(root.GetProperty("paths").GetProperty("/api/v1/profile").GetProperty("get").GetProperty("security").GetArrayLength() > 0);
        Assert.True(root.GetProperty("paths").GetProperty("/api/v1/management/ping").GetProperty("get").GetProperty("security").GetArrayLength() > 0);
        Assert.True(root.GetProperty("paths").GetProperty("/api/v1/notifications").GetProperty("get").GetProperty("security").GetArrayLength() > 0);
    }
}
