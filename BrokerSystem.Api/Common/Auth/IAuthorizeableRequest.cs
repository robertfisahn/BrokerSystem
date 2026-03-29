namespace BrokerSystem.Api.Common.Auth;

/// <summary>
/// Marker interface for MediatR requests that require authentication/authorization.
/// Requests implementing this will be intercepted by AuthorizationBehavior.
/// </summary>
public interface IAuthorizeableRequest
{
}
