namespace UltimateSolution.Application.Interfaces;

public interface IUserDirectory
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}
