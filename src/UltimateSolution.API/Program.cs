using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.IdentityModel.Tokens;
using UltimateSolution.API.Common.Authorization;
using UltimateSolution.API.Middlewares;
using UltimateSolution.Application.DependencyInjection;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Infrastructure.DependencyInjection;
using UltimateSolution.Infrastructure.Identity;
using UltimateSolution.Infrastructure.SignalR;
using UltimateSolution.API.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var jwtConfiguration = builder.Configuration.GetSection(JwtOptions.SectionName);
var issuer = jwtConfiguration["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer must be configured.");
var audience = jwtConfiguration["Audience"] ?? throw new InvalidOperationException("Jwt:Audience must be configured.");
var key = jwtConfiguration["Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured.");

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
});
builder.Services.AddSignalR();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var isRealtimeHubRequest = context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat")
                    || context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications");
                if (!string.IsNullOrWhiteSpace(accessToken) && isRealtimeHubRequest)
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationMiddlewareResultHandler>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationsHub>("/hubs/notifications");

using (var scope = app.Services.CreateScope())
{
    var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentitySeeder>();
    await identitySeeder.SeedAsync();
}

app.Run();

public partial class Program;
