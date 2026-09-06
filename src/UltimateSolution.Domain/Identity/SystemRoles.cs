namespace UltimateSolution.Domain.Identity;

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
    public const string ProjectManager = "ProjectManager";
    public const string HR = "HR";

    public static IReadOnlyCollection<string> All { get; } = [Admin, Manager, Employee, ProjectManager, HR];
}
