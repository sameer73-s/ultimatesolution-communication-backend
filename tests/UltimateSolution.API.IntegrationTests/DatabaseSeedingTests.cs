using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using UltimateSolution.Domain.Identity;
using Xunit;

namespace UltimateSolution.API.IntegrationTests;

public sealed class DatabaseSeedingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DatabaseSeedingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApplicationStartSeedsAllSystemRoles()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in SystemRoles.All)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            Assert.True(roleExists, $"The system role '{roleName}' should be seeded in the database.");
        }
    }
}
