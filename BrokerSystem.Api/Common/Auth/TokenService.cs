using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BrokerSystem.Api.Common.Auth;

public class TokenService(IConfiguration config, IHttpContextAccessor httpContextAccessor) : ITokenService
{
    private const string RefreshTokenCookieName = "broker_system_refresh_token";

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user, string primaryRole)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("userId", user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, primaryRole),
        };

        if (user.AgentId.HasValue)
            claims.Add(new System.Security.Claims.Claim("agentId", user.AgentId.Value.ToString()));

        var secret = config["Jwt:Secret"] ??
                     throw new InvalidOperationException("Klucz JWT Secret nie został skonfigurowany.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresMins = int.TryParse(config["Jwt:AccessTokenExpiresInMinutes"], out var mins) ? mins : 15;
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMins);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public RefreshToken GenerateRefreshToken(int userId)
    {
        var expiresDays = int.TryParse(config["Jwt:RefreshTokenExpiresInDays"], out var days) ? days : 7;

        return new RefreshToken
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expiresDays)
        };
    }

    public void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        };

        httpContextAccessor.HttpContext?.Response.Cookies.Append(RefreshTokenCookieName, token, cookieOptions);
    }

    public void RemoveRefreshTokenCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(-1)
        };

        httpContextAccessor.HttpContext?.Response.Cookies.Delete(RefreshTokenCookieName, cookieOptions);
    }

    public string? GetRefreshTokenFromCookie()
    {
        return httpContextAccessor.HttpContext?.Request.Cookies[RefreshTokenCookieName];
    }
}
