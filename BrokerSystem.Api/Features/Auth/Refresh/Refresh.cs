using BrokerSystem.Api.Common.Endpoints;
using BrokerSystem.Api.Common.Auth;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Features.Auth.Refresh;

public class RefreshEndpoint : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/refresh", async (IMediator mediator) =>
                Results.Ok(await mediator.Send(new RefreshCommand())))
            .WithName("RefreshToken")
            .WithTags("Auth")
            .AllowAnonymous();
    }
}

public record RefreshCommand : IRequest<RefreshResponseDto>;

public record RefreshResponseDto(string Token, DateTime ExpiresAt);

public class RefreshHandler(BrokerSystemDbContext db, ITokenService tokenService)
    : IRequestHandler<RefreshCommand, RefreshResponseDto>
{
    public async Task<RefreshResponseDto> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenString = tokenService.GetRefreshTokenFromCookie();

        if (string.IsNullOrEmpty(refreshTokenString))
            throw new Common.Exceptions.BadRequestException("Brak tokenu odświeżania.");

        var storedToken = await db.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.Token == refreshTokenString, cancellationToken);

        if (storedToken == null || !storedToken.IsActive)
            throw new Common.Exceptions.BadRequestException("Nieprawidłowy lub wygasły token odświeżania.");

        storedToken.RevokedAt = DateTime.UtcNow;

        var user = storedToken.User;
        var roleName = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Agent";

        var (tokenString, expiresAt) = tokenService.GenerateAccessToken(user, roleName);
        var newRefreshToken = tokenService.GenerateRefreshToken(user.UserId);

        db.RefreshTokens.Add(newRefreshToken);
        tokenService.SetRefreshTokenCookie(newRefreshToken.Token, newRefreshToken.ExpiresAt);

        await db.SaveChangesAsync(cancellationToken);

        return new RefreshResponseDto(tokenString, expiresAt);
    }
}
