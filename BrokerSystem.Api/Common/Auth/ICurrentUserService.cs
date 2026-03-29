using System.Security.Claims;

namespace BrokerSystem.Api.Common.Auth;

/// <summary>
/// Provides access to the currently authenticated user's identity data,
/// extracted from the incoming JWT token claims.
/// </summary>
public interface ICurrentUserService
{
    int UserId { get; }
    int? AgentId { get; }
    string Role { get; }
    bool IsAdmin { get; }
    bool IsAgent { get; }
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    public int UserId =>
        int.TryParse(_user?.FindFirstValue("userId"), out var id) ? id : 0;

    public int? AgentId
    {
        get
        {
            var val = _user?.FindFirstValue("agentId");
            return int.TryParse(val, out var id) ? id : null;
        }
    }

    public string Role
    {
        get
        {
            return _user?.FindFirstValue("role")
                   ?? _user?.FindFirstValue(ClaimTypes.Role)
                   ?? string.Empty;
        }
    }

    public bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    public bool IsAgent => Role.Equals("Agent", StringComparison.OrdinalIgnoreCase);
}
