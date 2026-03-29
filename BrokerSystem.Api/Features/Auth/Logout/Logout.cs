using BrokerSystem.Api.Common.Endpoints;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BrokerSystem.Api.Common.Auth;

namespace BrokerSystem.Api.Features.Auth.Logout;

public class LogoutEndpoint : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/logout", async (IMediator mediator) =>
            {
                await mediator.Send(new LogoutCommand());
                return Results.NoContent();
            })
            .WithName("Logout")
            .WithTags("Auth")
            .RequireAuthorization();
    }
}

public record LogoutCommand : IRequest;

public class LogoutHandler(
    BrokerSystemDbContext db,
    ITokenService tokenService)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenString = tokenService.GetRefreshTokenFromCookie();

        if (!string.IsNullOrEmpty(refreshTokenString))
        {
            var storedToken = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshTokenString, cancellationToken);

            if (storedToken != null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        tokenService.RemoveRefreshTokenCookie();
    }
}
