using BrokerSystem.Api.Infrastructure.Persistence.Context;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BrokerSystem.Api.Common.Endpoints;
using BrokerSystem.Api.Common.Auth;

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

public class GetClientsStatsHandler(BrokerSystemDbContext db, ICurrentUserService currentUserService)
    : IRequestHandler<GetClientsStatsQuery, ClientsStatsDto>
{
    public async Task<ClientsStatsDto> Handle(GetClientsStatsQuery request, CancellationToken cancellationToken)
    {
        using var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        int? effectiveAgentId = null;
        var whereClause = "";
        var policyFilter = "";

        if (!currentUserService.IsAdmin)
        {
            if (currentUserService.IsAgent)
            {
                // Agenci widzą tylko statystyki swoich klientów i polis
                effectiveAgentId = currentUserService.AgentId;
                whereClause =
                    "WHERE EXISTS (SELECT 1 FROM policies p WHERE p.client_id = c.client_id AND p.agent_id = @AgentId)";
                policyFilter = "AND agent_id = @AgentId";
            }
            else
            {
                // Użytkownicy bez przypisanej roli agenta/admina nie widzą nic
                whereClause = "WHERE 1=0";
                policyFilter = "AND 1=0";
            }
        }

        var sql = $@"
            SELECT 
                COUNT(*) AS TotalClients,
                COUNT(CASE WHEN ct.type_name = 'VIP' THEN 1 END) AS VipClients,
                COUNT(CASE WHEN ct.type_name = 'Corporate' THEN 1 END) AS CorporateClients,
                COUNT(CASE WHEN c.registration_date >= @StartOfMonth THEN 1 END) AS NewClientsThisMonth,
                (SELECT COUNT(*) FROM policies p JOIN policy_statuses ps ON p.status_id = ps.status_id WHERE ps.is_active_policy = 1 {policyFilter}) AS ActivePoliciesTotal
            FROM clients c
            JOIN client_types ct ON c.client_type_id = ct.client_type_id
            {whereClause}";

        var parameters = new { StartOfMonth = startOfMonth, AgentId = effectiveAgentId };
        var stats = await connection.QuerySingleOrDefaultAsync<ClientsStatsDto>(sql, parameters);

        return stats ?? new ClientsStatsDto();
    }
}
