using BrokerSystem.Api.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Features.Clients.GetClientsStats;

public record GetClientsStatsQuery() : IRequest<ClientsStatsDto>;

public record ClientsStatsDto
{
    public int TotalClients { get; init; }
    public int VipClients { get; init; }
    public int CorporateClients { get; init; }
    public int ActivePoliciesTotal { get; init; }
    public int NewClientsThisMonth { get; init; }
}

public class GetClientsStatsHandler(BrokerSystemDbContext db) : IRequestHandler<GetClientsStatsQuery, ClientsStatsDto>
{
    public async Task<ClientsStatsDto> Handle(GetClientsStatsQuery request, CancellationToken cancellationToken)
    {
        var startOfMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);

        var stats = await db.Clients
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new ClientsStatsDto
            {
                TotalClients = g.Count(),
                VipClients = g.Count(c => c.ClientType.TypeName == "VIP"),
                CorporateClients = g.Count(c => c.ClientType.TypeName == "Corporate"),
                ActivePoliciesTotal = g.Sum(c => c.Policies.Count(p => p.Status.IsActivePolicy)),
                NewClientsThisMonth = g.Count(c => c.RegistrationDate >= startOfMonth)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return stats ?? new ClientsStatsDto();
    }
}
