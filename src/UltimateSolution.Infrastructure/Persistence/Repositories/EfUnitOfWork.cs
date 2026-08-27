using UltimateSolution.Application.Interfaces;

namespace UltimateSolution.Infrastructure.Persistence.Repositories;

public sealed class EfUnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
