using BrokerSystem.Api.Infrastructure.Persistence.Context;
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
        var clients = await db.Clients
            .Where(c => c.IsActive)
            .OrderBy(c => c.LastName ?? c.CompanyName)
            .Select(c => new LookupDto(c.ClientId, 
                !string.IsNullOrWhiteSpace(c.CompanyName) ? c.CompanyName :
                !string.IsNullOrWhiteSpace(c.FirstName + c.LastName) ? ((c.FirstName ?? "") + " " + (c.LastName ?? "")).Trim() :
                $"Client #{c.ClientId}"))
            .ToListAsync(ct);

        var policyTypes = await db.PolicyTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.TypeName)
            .Select(t => new LookupDto(t.PolicyTypeId, t.TypeName))
            .ToListAsync(ct);

        var agents = await db.Agents
            .Where(a => a.IsActive)
            .OrderBy(a => a.LastName)
            .Select(a => new LookupDto(a.AgentId, 
                !string.IsNullOrWhiteSpace(a.FirstName + a.LastName) ? ((a.FirstName ?? "") + " " + (a.LastName ?? "")).Trim() :
                $"Agent #{a.AgentId}"))
            .ToListAsync(ct);

        return new PolicyLookupsResponse(clients, policyTypes, agents);
    }
}
