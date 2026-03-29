using BrokerSystem.Api.Common.Auth;
using BrokerSystem.Api.Common.Exceptions;
using MediatR;

namespace BrokerSystem.Api.Common.Middleware;

/// <summary>
/// MediatR Pipeline Behavior that enforces authorization for requests implementing IAuthorizeableRequest.
/// It verifies if the user is authenticated using ICurrentUserService.
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse>(ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IAuthorizeableRequest)
        {
            if (currentUserService.UserId == 0)
            {
                throw new ForbidException("Wymagane jest uwierzytelnienie, aby wykonać tę akcję.");
            }

            // Optional: check roles if IAuthorizeableRequest had role requirements
            // if (request is IRoleRestrictedRequest roleRequest && !roleRequest.Roles.Contains(currentUserService.Role))
            //     throw new ForbidException("Insufficient permissions.");
        }

        return await next();
    }
}
