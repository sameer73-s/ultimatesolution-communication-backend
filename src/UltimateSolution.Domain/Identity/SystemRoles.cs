namespace UltimateSolution.Domain.Identity;

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    public static IReadOnlyCollection<string> All { get; } = [Admin, Manager, Employee];
}
