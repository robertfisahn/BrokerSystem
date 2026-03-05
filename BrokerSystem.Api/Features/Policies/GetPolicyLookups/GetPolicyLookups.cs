using BrokerSystem.Api.Infrastructure.Persistence.Context;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

using BrokerSystem.Api.Common.Endpoints;

namespace BrokerSystem.Api.Features.Policies.GetPolicyLookups;

public class GetPolicyLookupsEndpoint : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("api/policies/lookups", async (IMediator mediator) => 
            Results.Ok(await mediator.Send(new GetPolicyLookupsQuery())))
            .WithName("GetPolicyLookups")
            .WithTags("Policies");
    }
}

public record GetPolicyLookupsQuery : IRequest<PolicyLookupsResponse>;

public record PolicyLookupsResponse(
    List<LookupDto> Clients,
    List<LookupDto> PolicyTypes,
    List<LookupDto> Agents);

public class LookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public LookupDto() { }
    public LookupDto(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

public class GetPolicyLookupsHandler(BrokerSystemDbContext db) : IRequestHandler<GetPolicyLookupsQuery, PolicyLookupsResponse>
{
    public async Task<PolicyLookupsResponse> Handle(GetPolicyLookupsQuery request, CancellationToken ct)
    {
        using var connection = db.Database.GetDbConnection();

        const string sql = @"
            -- Clients
            SELECT client_id AS Id, first_name AS FirstName, last_name AS LastName, company_name AS CompanyName 
            FROM clients WHERE is_active = 1 ORDER BY last_name, company_name;

            -- PolicyTypes
            SELECT policy_type_id AS Id, type_name AS Name 
            FROM policy_types WHERE is_active = 1 ORDER BY type_name;

            -- Agents
            SELECT agent_id AS Id, first_name AS FirstName, last_name AS LastName 
            FROM agents WHERE is_active = 1 ORDER BY last_name;";

        using var multi = await connection.QueryMultipleAsync(sql);

        var rawClients = await multi.ReadAsync<dynamic>();
        var clients = (rawClients as IEnumerable<dynamic>).Select(c => (LookupDto)LookupMapper.MapClient(c)).ToList();

        var policyTypes = (await multi.ReadAsync<LookupDto>()).ToList();

        var rawAgents = await multi.ReadAsync<dynamic>();
        var agents = (rawAgents as IEnumerable<dynamic>).Select(a => (LookupDto)LookupMapper.MapAgent(a)).ToList();

        return new PolicyLookupsResponse(clients, policyTypes, agents);
    }
}

public static class LookupMapper
{
    public static LookupDto MapClient(dynamic c)
    {
        int id = Convert.ToInt32(c.Id);
        string? companyName = (string?)c.CompanyName;
        string? firstName = (string?)c.FirstName;
        string? lastName = (string?)c.LastName;

        string name = !string.IsNullOrWhiteSpace(companyName) ? companyName :
                     !string.IsNullOrWhiteSpace(firstName + lastName) ? (firstName?.Trim() + " " + lastName?.Trim()).Trim() :
                     $"Client #{id}";

        return new LookupDto(id, name);
    }

    public static LookupDto MapAgent(dynamic a)
    {
        int id = Convert.ToInt32(a.Id);
        string? firstName = (string?)a.FirstName;
        string? lastName = (string?)a.LastName;

        string name = !string.IsNullOrWhiteSpace(firstName + lastName) ? (firstName?.Trim() + " " + lastName?.Trim()).Trim() :
                     $"Agent #{id}";

        return new LookupDto(id, name);
    }
}
