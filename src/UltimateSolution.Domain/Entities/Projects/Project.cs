using UltimateSolution.Domain.Enums;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.Domain.Entities.Projects;

public sealed class Project
{
    private Project()
    {
    }

    private Project(Guid id, string name, Guid ownerUserId, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        OwnerUserId = ownerUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid OwnerUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<ProjectMember> Members { get; } = new List<ProjectMember>();

    public static Project Create(string name, Guid ownerUserId, DateTimeOffset createdAtUtc)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new DomainValidationException("A project owner is required.");
        }

        var normalizedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new DomainValidationException("A project name is required.");
        }

        if (normalizedName.Length > 120)
        {
            throw new DomainValidationException("Project name cannot exceed 120 characters.");
        }

        var project = new Project(Guid.NewGuid(), normalizedName, ownerUserId, createdAtUtc);
        
        // The owner is implicitly a Manager member
        project.Members.Add(new ProjectMember(project.Id, ownerUserId, ProjectMemberRole.Manager));

        return project;
    }
}
