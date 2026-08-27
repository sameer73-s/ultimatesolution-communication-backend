using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Infrastructure.Identity;

namespace UltimateSolution.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entityBuilder =>
        {
            entityBuilder.Property(user => user.DisplayName).HasMaxLength(150).IsRequired();
        });

        builder.Entity<RefreshToken>(entityBuilder =>
        {
            entityBuilder.HasKey(token => token.Id);
            entityBuilder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
            entityBuilder.HasIndex(token => token.TokenHash).IsUnique();
            entityBuilder.HasIndex(token => new { token.UserId, token.ExpiresAtUtc });
        });

    }
}
