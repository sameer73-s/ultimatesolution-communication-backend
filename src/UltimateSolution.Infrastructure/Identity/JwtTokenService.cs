using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UltimateSolution.Application.Features.Identity;
using UltimateSolution.Infrastructure.Persistence;

namespace UltimateSolution.Infrastructure.Identity;

public sealed class JwtTokenService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public async Task<AuthTokenResponse> CreateTokenPairAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAtUtc = now.AddMinutes(_options.AccessTokenMinutes);
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = CreateAccessToken(user, roles, expiresAtUtc);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(refreshToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_options.RefreshTokenDays)
        });
        await context.SaveChangesAsync(cancellationToken);

        return new AuthTokenResponse(accessToken, refreshToken, expiresAtUtc, roles.ToArray());
    }

    public async Task<AuthTokenResponse> RefreshAsync(string suppliedRefreshToken, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var tokenHash = Hash(suppliedRefreshToken);
        var refreshToken = await context.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive(now))
        {
            throw new UnauthorizedAccessException();
        }

        var user = await userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user is null)
        {
            throw new UnauthorizedAccessException();
        }

        refreshToken.RevokedAtUtc = now;
        await context.SaveChangesAsync(cancellationToken);

        return await CreateTokenPairAsync(user, cancellationToken);
    }

    private string CreateAccessToken(
        ApplicationUser user,
        IEnumerable<string> roles,
        DateTimeOffset expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Audience = _options.Audience,
            Issuer = _options.Issuer,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = credentials
        };

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
