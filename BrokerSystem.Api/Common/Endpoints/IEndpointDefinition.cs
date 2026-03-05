namespace BrokerSystem.Api.Common.Endpoints;

public interface IEndpointDefinition
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
