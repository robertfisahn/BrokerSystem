using BrokerSystem.Api.Infrastructure.Persistence.Context;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

using BrokerSystem.Api.Common.Endpoints;

namespace BrokerSystem.Api.Features.Clients.GetClientsStats;

public class GetClientsStatsEndpoint : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("api/clients/stats", async (IMediator mediator) => 
            Results.Ok(await mediator.Send(new GetClientsStatsQuery())))
            .WithName("GetClientsStats")
            .WithTags("Clients");
    }
}

/// <summary>
/// Query to retrieve high-level client statistics for dashboard cards.
/// </summary>
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
    public const string GetStatsSql = @"
            SELECT 
                (SELECT COUNT(*) FROM clients) as TotalClients,
                (SELECT COUNT(*) FROM clients c JOIN client_types ct ON c.client_type_id = ct.client_type_id WHERE ct.type_name = 'VIP') as VipClients,
                (SELECT COUNT(*) FROM clients c JOIN client_types ct ON c.client_type_id = ct.client_type_id WHERE ct.type_name = 'Corporate') as CorporateClients,
                (SELECT COUNT(*) FROM policies p JOIN policy_statuses ps ON p.status_id = ps.status_id WHERE ps.is_active_policy = 1) as ActivePoliciesTotal,
                (SELECT COUNT(*) FROM clients WHERE registration_date >= @StartOfMonth) as NewClientsThisMonth";

    public async Task<ClientsStatsDto> Handle(GetClientsStatsQuery request, CancellationToken cancellationToken)
    {
        using var connection = db.Database.GetDbConnection();
        var startOfMonth = CalculateStartOfMonth(DateTime.Today);

        var stats = await connection.QuerySingleOrDefaultAsync<ClientsStatsDto>(GetStatsSql, new { StartOfMonth = startOfMonth });

        return MapResult(stats);
    }

    /// <summary>
    /// Pure logic to calculate the first day of the current month.
    /// </summary>
    public static DateTime CalculateStartOfMonth(DateTime date) => new DateTime(date.Year, date.Month, 1);

    /// <summary>
    /// Pure logic to ensure a non-null response.
    /// </summary>
    public static ClientsStatsDto MapResult(ClientsStatsDto? stats) => stats ?? new ClientsStatsDto();
}
