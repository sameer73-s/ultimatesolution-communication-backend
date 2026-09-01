using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Chat;
using UltimateSolution.Domain.Identity;
using UltimateSolution.Infrastructure.Persistence;

namespace UltimateSolution.Infrastructure.Security;

public sealed class ActionItemAuthorizationService(ApplicationDbContext context) : IActionItemAuthorizationService
{
    public async Task<bool> CanConvertMessageToActionItemAsync(Guid userId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        // SystemAdmins can do anything
        var user = await context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null) return false;
        
        var userRoles = await context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == SystemRoles.Admin, cancellationToken);
        
        if (adminRole != null && userRoles.Any(ur => ur.RoleId == adminRole.Id))
            return true;

        // Otherwise, check if user is in the channel
        var isMember = await context.Set<ChannelMember>()
            .AnyAsync(cm => cm.ChannelId == message.ChannelId && cm.UserId == userId, cancellationToken);
            
        return isMember;
    }
}
