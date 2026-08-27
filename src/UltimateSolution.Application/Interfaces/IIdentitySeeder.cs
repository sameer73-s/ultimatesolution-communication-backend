namespace UltimateSolution.Application.Interfaces;

public interface IIdentitySeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
