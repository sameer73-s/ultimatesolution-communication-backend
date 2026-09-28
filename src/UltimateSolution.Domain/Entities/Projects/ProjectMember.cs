using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Domain.Entities.Projects;

public sealed class ProjectMember
{
    private ProjectMember()
    {
    }

    internal ProjectMember(Guid projectId, Guid userId, ProjectMemberRole role)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
    }

    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectMemberRole Role { get; private set; }

    public Project? Project { get; private set; }
}
