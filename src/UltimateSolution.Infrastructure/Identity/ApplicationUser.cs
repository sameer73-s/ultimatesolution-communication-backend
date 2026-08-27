using Microsoft.AspNetCore.Identity;

namespace UltimateSolution.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
