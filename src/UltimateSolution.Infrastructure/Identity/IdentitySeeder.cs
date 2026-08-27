using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Identity;
using UltimateSolution.Infrastructure.Persistence;

namespace UltimateSolution.Infrastructure.Identity;

public sealed class IdentitySeeder(
    ApplicationDbContext context,
    RoleManager<IdentityRole<Guid>> roleManager)
    : IIdentitySeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        foreach (var roleName in SystemRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                });
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Unable to seed role '{roleName}'.");
                }
            }
        }
    }
}
