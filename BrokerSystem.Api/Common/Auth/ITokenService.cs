using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using System.Security.Claims;

namespace BrokerSystem.Api.Common.Auth;

/// <summary>
/// Service for centralized handling of JWT and Refresh Token operations.
/// </summary>
public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user, string primaryRole);
    RefreshToken GenerateRefreshToken(int userId);
    void SetRefreshTokenCookie(string token, DateTime expiresAt);
    void RemoveRefreshTokenCookie();
    string? GetRefreshTokenFromCookie();
}
