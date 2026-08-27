using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Chat;
using UltimateSolution.Domain.Enums;
using UltimateSolution.Infrastructure.Identity;

namespace UltimateSolution.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ChatChannel> ChatChannels => Set<ChatChannel>();

    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<MessageReadState> MessageReadStates => Set<MessageReadState>();

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

        builder.Entity<ChatChannel>(entityBuilder =>
        {
            entityBuilder.ToTable("ChatChannels");
            entityBuilder.HasKey(channel => channel.Id);
            entityBuilder.Property(channel => channel.Name).HasMaxLength(120).IsRequired();
            entityBuilder.Property(channel => channel.Type).HasConversion<string>().HasMaxLength(16).IsRequired();
            entityBuilder.HasIndex(channel => new { channel.CreatedByUserId, channel.IsArchived });
            entityBuilder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(channel => channel.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entityBuilder.HasMany(channel => channel.Members)
                .WithOne()
                .HasForeignKey(member => member.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);
            entityBuilder.HasMany(channel => channel.Messages)
                .WithOne()
                .HasForeignKey(message => message.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChannelMember>(entityBuilder =>
        {
            entityBuilder.ToTable("ChannelMembers");
            entityBuilder.HasKey(member => new { member.ChannelId, member.UserId });
            entityBuilder.Property(member => member.Role).HasConversion<string>().HasMaxLength(16).IsRequired();
            entityBuilder.HasIndex(member => member.UserId);
            entityBuilder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ChatMessage>(entityBuilder =>
        {
            entityBuilder.ToTable("ChatMessages");
            entityBuilder.HasKey(message => message.Id);
            entityBuilder.Property(message => message.Body).HasMaxLength(4000).IsRequired();
            entityBuilder.HasIndex(message => new { message.ChannelId, message.CreatedAtUtc });
            entityBuilder.HasIndex(message => new { message.SenderUserId, message.CreatedAtUtc });
            entityBuilder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(message => message.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MessageReadState>(entityBuilder =>
        {
            entityBuilder.ToTable("MessageReadStates");
            entityBuilder.HasKey(readState => new { readState.ChannelId, readState.UserId });
            entityBuilder.HasIndex(readState => readState.UserId);
            entityBuilder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(readState => readState.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
