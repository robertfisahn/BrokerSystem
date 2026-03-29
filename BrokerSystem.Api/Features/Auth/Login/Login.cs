using BrokerSystem.Api.Common.Auth;
using BrokerSystem.Api.Common.Endpoints;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Features.Auth.Login;

public class LoginEndpoint : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/login", async (IMediator mediator, LoginCommand command) =>
                Results.Ok(await mediator.Send(command)))
            .WithName("Login")
            .WithTags("Auth")
            .AllowAnonymous();
    }
}

public record LoginCommand(string Username, string Password) : IRequest<LoginResponseDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public record LoginResponseDto
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string? DisplayName { get; init; }
    public string Role { get; init; } = string.Empty;
    public int? AgentId { get; init; }
}

public class LoginHandler(BrokerSystemDbContext db, ITokenService tokenService)
    : IRequestHandler<LoginCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Agent)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Common.Exceptions.BadRequestException("Nieprawidłowa nazwa użytkownika lub hasło.");

        var roles = user.UserRoles.Select(ur => ur.Role?.RoleName).Where(r => r != null).Cast<string>().ToList();
        var roleName = roles.Contains("Admin") ? "Admin" : (roles.FirstOrDefault() ?? "Agent");

        var (tokenString, expiresAt) = tokenService.GenerateAccessToken(user, roleName);
        var refreshToken = tokenService.GenerateRefreshToken(user.UserId);

        db.RefreshTokens.Add(refreshToken);
        tokenService.SetRefreshTokenCookie(refreshToken.Token, refreshToken.ExpiresAt);

        user.LastLogin = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            Token = tokenString,
            ExpiresAt = expiresAt,
            DisplayName = user.Agent is not null
                ? $"{user.Agent.FirstName} {user.Agent.LastName}"
                : user.Username,
            Role = roleName,
            AgentId = user.AgentId
        };
    }
}
