using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UltimateSolution.Application.Features.Identity;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Infrastructure.ExternalServices.Ai;
using UltimateSolution.Infrastructure.ExternalServices.Meetings;
using UltimateSolution.Infrastructure.Identity;
using UltimateSolution.Infrastructure.Persistence;
using UltimateSolution.Infrastructure.Persistence.Repositories;
using UltimateSolution.Infrastructure.Presence;
using UltimateSolution.Infrastructure.SignalR;

namespace UltimateSolution.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var connectionString = configuration["ConnectionStrings:DefaultConnection"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
        }

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer must be configured.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience must be configured.")
            .Validate(options => options.Key.Length >= 32, "Jwt:Key must be at least 32 characters.")
            .Validate(options => options.AccessTokenMinutes is > 0 and <= 60, "Jwt:AccessTokenMinutes must be between 1 and 60.")
            .Validate(options => options.RefreshTokenDays is > 0 and <= 30, "Jwt:RefreshTokenDays must be between 1 and 30.")
            .ValidateOnStart();

        services.AddOptions<JitsiMeetingMediaOptions>()
            .Bind(configuration.GetSection(JitsiMeetingMediaOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "MeetingMedia:BaseUrl must be an absolute URL.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.AppId), "MeetingMedia:AppId must be configured.")
            .Validate(options => options.ApiSecret.Length >= 32, "MeetingMedia:ApiSecret must be at least 32 characters.")
            .Validate(options => options.JoinUrlLifetimeMinutes is > 0 and <= 60, "MeetingMedia:JoinUrlLifetimeMinutes must be between 1 and 60.")
            .ValidateOnStart();

        services.AddOptions<ExternalMeetingIntelligenceOptions>()
            .Bind(configuration.GetSection(ExternalMeetingIntelligenceOptions.SectionName));

        var useInMemoryDatabase = environment.IsEnvironment("Testing")
            || configuration.GetValue<bool>("Persistence:UseInMemory");
        var inMemoryDatabaseName = configuration["Persistence:InMemoryDatabaseName"]
            ?? (environment.IsEnvironment("Testing")
                ? $"UltimateSolutionCommunicationTests-{Guid.NewGuid():N}"
                : "UltimateSolutionCommunicationTests");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (useInMemoryDatabase)
            {
                options.UseInMemoryDatabase(inMemoryDatabaseName);
                return;
            }

            options.UseSqlServer(connectionString);
        });
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IChatChannelRepository, ChatChannelRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddSingleton<IPresenceTracker, InMemoryPresenceTracker>();
        services.AddScoped<IChatRealtimePublisher, SignalRChatRealtimePublisher>();
        services.AddScoped<IMeetingRepository, MeetingRepository>();
        services.AddScoped<IMeetingIntelligenceRepository, MeetingIntelligenceRepository>();
        services.AddScoped<IActionItemRepository, ActionItemRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();
        services.AddSingleton<IMeetingMediaService, JitsiMeetingMediaService>();
        services.AddScoped<IMeetingSummaryApprovalPolicy, OrganizerOrManagerMeetingSummaryApprovalPolicy>();
        if (environment.IsEnvironment("Testing"))
        {
            services.AddSingleton<TestMeetingIntelligenceService>();
            services.AddScoped<ITranscriptionService>(provider => provider.GetRequiredService<TestMeetingIntelligenceService>());
            services.AddScoped<ISummaryService>(provider => provider.GetRequiredService<TestMeetingIntelligenceService>());
        }
        else
        {
            services.AddHttpClient<ExternalMeetingIntelligenceService>();
            services.AddScoped<ITranscriptionService>(provider => provider.GetRequiredService<ExternalMeetingIntelligenceService>());
            services.AddScoped<ISummaryService>(provider => provider.GetRequiredService<ExternalMeetingIntelligenceService>());
        }

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<JwtTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserDirectory, IdentityUserDirectory>();
        services.AddScoped<IIdentitySeeder, IdentitySeeder>();

        return services;
    }
}
