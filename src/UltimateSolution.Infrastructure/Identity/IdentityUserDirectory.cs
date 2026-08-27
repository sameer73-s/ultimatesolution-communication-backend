using Microsoft.AspNetCore.Identity;
using UltimateSolution.Application.Interfaces;

namespace UltimateSolution.Infrastructure.Identity;

public sealed class IdentityUserDirectory(UserManager<ApplicationUser> userManager) : IUserDirectory
{
    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await userManager.FindByIdAsync(userId.ToString()) is not null;
    }
}
