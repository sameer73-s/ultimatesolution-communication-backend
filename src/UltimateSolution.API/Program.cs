using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UltimateSolution.API.Middlewares;
using UltimateSolution.Application.DependencyInjection;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Infrastructure.DependencyInjection;
using UltimateSolution.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

var jwtConfiguration = builder.Configuration.GetSection(JwtOptions.SectionName);
var issuer = jwtConfiguration["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer must be configured.");
var audience = jwtConfiguration["Audience"] ?? throw new InvalidOperationException("Jwt:Audience must be configured.");
var key = jwtConfiguration["Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured.");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
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
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentitySeeder>();
    await identitySeeder.SeedAsync();
}

app.Run();

public partial class Program;
