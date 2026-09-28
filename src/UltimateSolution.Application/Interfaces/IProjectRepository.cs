using UltimateSolution.Domain.Entities.Projects;

namespace UltimateSolution.Application.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMember>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default);
}
