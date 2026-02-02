using BrokerSystem.Api.Infrastructure.Persistence.Context;
using Dapper;
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
        using var connection = db.Database.GetDbConnection();

        const string sql = @"
            DECLARE @StartOfMonth DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);

            SELECT 
                (SELECT COUNT(*) FROM clients) as TotalClients,
                (SELECT COUNT(*) FROM clients c JOIN client_types ct ON c.client_type_id = ct.client_type_id WHERE ct.type_name = 'VIP') as VipClients,
                (SELECT COUNT(*) FROM clients c JOIN client_types ct ON c.client_type_id = ct.client_type_id WHERE ct.type_name = 'Corporate') as CorporateClients,
                (SELECT COUNT(*) FROM policies p JOIN policy_statuses ps ON p.status_id = ps.status_id WHERE ps.is_active_policy = 1) as ActivePoliciesTotal,
                (SELECT COUNT(*) FROM clients WHERE registration_date >= @StartOfMonth) as NewClientsThisMonth";

        var stats = await connection.QuerySingleOrDefaultAsync<ClientsStatsDto>(sql);

        return stats ?? new ClientsStatsDto();
    }
}
