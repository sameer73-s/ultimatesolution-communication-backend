using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Projects;

namespace UltimateSolution.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository(ApplicationDbContext context) : IProjectRepository
{
    public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        context.Projects.SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken);

    public async Task<IReadOnlyList<ProjectMember>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await context.Set<ProjectMember>()
            .AsNoTracking()
            .Where(member => member.ProjectId == projectId)
            .ToListAsync(cancellationToken);
}
