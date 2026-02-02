using BrokerSystem.Api.Infrastructure.Persistence.Context;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Features.Policies.GetPolicyLookups;

public record GetPolicyLookupsQuery : IRequest<PolicyLookupsResponse>;

public record PolicyLookupsResponse(
    List<LookupDto> Clients,
    List<LookupDto> PolicyTypes,
    List<LookupDto> Agents);

public record LookupDto(int Id, string Name);

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
        var clients = rawClients.Select(c => new LookupDto(
            (int)c.Id,
            !string.IsNullOrWhiteSpace((string?)c.CompanyName) ? (string)c.CompanyName :
            !string.IsNullOrWhiteSpace((string?)c.FirstName + (string?)c.LastName) ? (((string?)c.FirstName ?? "") + " " + ((string?)c.LastName ?? "")).Trim() :
            $"Client #{c.Id}"
        )).ToList();

        var policyTypes = (await multi.ReadAsync<LookupDto>()).ToList();

        var rawAgents = await multi.ReadAsync<dynamic>();
        var agents = rawAgents.Select(a => new LookupDto(
            (int)a.Id,
            !string.IsNullOrWhiteSpace((string?)a.FirstName + (string?)a.LastName) ? (((string?)a.FirstName ?? "") + " " + ((string?)a.LastName ?? "")).Trim() :
            $"Agent #{a.Id}"
        )).ToList();

        return new PolicyLookupsResponse(clients, policyTypes, agents);
    }
}
