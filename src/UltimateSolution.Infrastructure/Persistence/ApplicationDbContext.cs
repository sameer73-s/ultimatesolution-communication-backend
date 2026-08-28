using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Entities.Chat;
using UltimateSolution.Domain.Entities.Meetings;
using UltimateSolution.Domain.Entities.Notifications;
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

    public DbSet<Meeting> Meetings => Set<Meeting>();

    public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();

    public DbSet<MeetingRecording> MeetingRecordings => Set<MeetingRecording>();

    public DbSet<TranscriptionJob> TranscriptionJobs => Set<TranscriptionJob>();

    public DbSet<TranscriptionSegment> TranscriptionSegments => Set<TranscriptionSegment>();

    public DbSet<MeetingSummary> MeetingSummaries => Set<MeetingSummary>();

    public DbSet<ActionItem> ActionItems => Set<ActionItem>();

    public DbSet<Notification> Notifications => Set<Notification>();

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

        builder.Entity<Meeting>(entityBuilder =>
        {
            entityBuilder.ToTable("Meetings");
            entityBuilder.HasKey(meeting => meeting.Id);
            entityBuilder.Property(meeting => meeting.Title).HasMaxLength(180).IsRequired();
            entityBuilder.Property(meeting => meeting.Agenda).HasMaxLength(4000);
            entityBuilder.Property(meeting => meeting.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entityBuilder.Property(meeting => meeting.MediaSessionReference).HasMaxLength(200);
            entityBuilder.HasIndex(meeting => new { meeting.OrganizerUserId, meeting.ScheduledStartUtc });
            entityBuilder.HasIndex(meeting => new { meeting.Status, meeting.ScheduledStartUtc });
            entityBuilder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(meeting => meeting.OrganizerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entityBuilder.HasMany(meeting => meeting.Participants)
                .WithOne()
                .HasForeignKey(participant => participant.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            entityBuilder.HasMany(meeting => meeting.Recordings)
                .WithOne()
                .HasForeignKey(recording => recording.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MeetingParticipant>(entityBuilder =>
        {
            entityBuilder.ToTable("MeetingParticipants");
            entityBuilder.HasKey(participant => new { participant.MeetingId, participant.UserId });
            entityBuilder.Property(participant => participant.Role).HasConversion<string>().HasMaxLength(16).IsRequired();
            entityBuilder.HasIndex(participant => participant.UserId);
            entityBuilder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MeetingRecording>(entityBuilder =>
        {
            entityBuilder.ToTable("MeetingRecordings");
            entityBuilder.HasKey(recording => recording.Id);
            entityBuilder.Property(recording => recording.MediaRecordingReference).HasMaxLength(200).IsRequired();
            entityBuilder.Property(recording => recording.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entityBuilder.HasIndex(recording => new { recording.MeetingId, recording.Status });
            entityBuilder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(recording => recording.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TranscriptionJob>(entityBuilder =>
        {
            entityBuilder.ToTable("TranscriptionJobs");
            entityBuilder.HasKey(job => job.Id);
            entityBuilder.Property(job => job.MediaRecordingReference).HasMaxLength(200).IsRequired();
            entityBuilder.Property(job => job.ExternalJobReference).HasMaxLength(200);
            entityBuilder.Property(job => job.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entityBuilder.Property(job => job.FailureCode).HasMaxLength(100);
            entityBuilder.HasIndex(job => new { job.MeetingId, job.RequestedAtUtc });
            entityBuilder.HasIndex(job => new { job.RecordingId, job.Status });
            entityBuilder.HasOne<MeetingRecording>()
                .WithMany(recording => recording.TranscriptionJobs)
                .HasForeignKey(job => job.RecordingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TranscriptionSegment>(entityBuilder =>
        {
            entityBuilder.ToTable("TranscriptionSegments");
            entityBuilder.HasKey(segment => segment.Id);
            entityBuilder.Property(segment => segment.Text).HasMaxLength(4000).IsRequired();
            entityBuilder.Property(segment => segment.SpeakerLabel).HasMaxLength(120);
            entityBuilder.HasIndex(segment => new { segment.TranscriptionJobId, segment.SequenceNumber }).IsUnique();
            entityBuilder.HasOne<TranscriptionJob>()
                .WithMany(job => job.Segments)
                .HasForeignKey(segment => segment.TranscriptionJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MeetingSummary>(entityBuilder =>
        {
            entityBuilder.ToTable("MeetingSummaries");
            entityBuilder.HasKey(summary => summary.Id);
            entityBuilder.Property(summary => summary.Content).HasMaxLength(16000).IsRequired();
            entityBuilder.Property(summary => summary.DecisionsJson).HasMaxLength(16000).IsRequired();
            entityBuilder.Property(summary => summary.ProposedActionItemsJson).HasMaxLength(32000).IsRequired();
            entityBuilder.Property(summary => summary.ExternalSummaryReference).HasMaxLength(200);
            entityBuilder.Property(summary => summary.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entityBuilder.HasIndex(summary => new { summary.MeetingId, summary.GeneratedAtUtc });
            entityBuilder.HasIndex(summary => summary.TranscriptionJobId).IsUnique();
            entityBuilder.HasOne<Meeting>()
                .WithMany(meeting => meeting.Summaries)
                .HasForeignKey(summary => summary.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            entityBuilder.HasOne<TranscriptionJob>()
                .WithMany()
                .HasForeignKey(summary => summary.TranscriptionJobId)
                .OnDelete(DeleteBehavior.Restrict);
            entityBuilder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(summary => summary.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ActionItem>(entityBuilder =>
        {
            entityBuilder.ToTable("ActionItems");
            entityBuilder.HasKey(actionItem => actionItem.Id);
            entityBuilder.Property(actionItem => actionItem.Title).HasMaxLength(400).IsRequired();
            entityBuilder.Property(actionItem => actionItem.Description).HasMaxLength(4000);
            entityBuilder.Property(actionItem => actionItem.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entityBuilder.HasIndex(actionItem => new { actionItem.AssigneeUserId, actionItem.Status, actionItem.DueAtUtc });
            entityBuilder.HasIndex(actionItem => new { actionItem.MeetingSummaryId, actionItem.CreatedAtUtc });
            entityBuilder.HasOne<MeetingSummary>()
                .WithMany(summary => summary.ActionItems)
                .HasForeignKey(actionItem => actionItem.MeetingSummaryId)
                .OnDelete(DeleteBehavior.Cascade);
            entityBuilder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(actionItem => actionItem.AssigneeUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(entityBuilder =>
        {
            entityBuilder.ToTable("Notifications");
            entityBuilder.HasKey(notification => notification.Id);
            entityBuilder.Property(notification => notification.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            entityBuilder.Property(notification => notification.SourceType).HasMaxLength(100).IsRequired();
            entityBuilder.Property(notification => notification.Title).HasMaxLength(200).IsRequired();
            entityBuilder.Property(notification => notification.Body).HasMaxLength(2000);
            entityBuilder.HasIndex(notification => new { notification.RecipientUserId, notification.ReadAtUtc, notification.CreatedAtUtc });
            entityBuilder.HasIndex(notification => new { notification.SourceType, notification.SourceId });
            entityBuilder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(notification => notification.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
