using BrokerSystem.Api.Common.Auth;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Common.Caching;
using BrokerSystem.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Dapper;

using BrokerSystem.Api.Common.Endpoints;

namespace BrokerSystem.Api.Features.Dashboard;

public class GetDashboardStatsEndpoint : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("api/dashboard/stats", async (IMediator mediator) => 
            Results.Ok(await mediator.Send(new GetDashboardStatsQuery())))
            .WithName("GetDashboardStats")
            .WithTags("Dashboard")
            .RequireAuthorization(p => p.RequireRole("Admin"));
    }
}

/// <summary>
/// Query to retrieve aggregated dashboard statistics, including sales trends and distribution charts.
/// </summary>
public record GetDashboardStatsQuery() : IRequest<DashboardStatsResponse>, IAuthorizeableRequest;

public class DashboardStatsResponse
{
    public List<MonthlySales> MonthlySales { get; set; } = new();
    public List<ClientTypeDistribution> ClientTypeDistribution { get; set; } = new();
    public List<PolicyStatusDistribution> PolicyStatusDistribution { get; set; } = new();
    public DashboardKpis Kpis { get; set; } = new();
}

public class MonthlySales
{
    public string Month { get; set; } = null!;
    public decimal TotalPremium { get; set; }
    public int PolicyCount { get; set; }
}

public class ClientTypeDistribution
{
    public string ClientType { get; set; } = null!;
    public int ClientCount { get; set; }
}

public class PolicyStatusDistribution
{
    public string PolicyStatus { get; set; } = null!;
    public int PolicyCount { get; set; }
}

public class DashboardKpis
{
    public int TotalClients { get; set; }
    public int TotalPolicies { get; set; }
    public int ActiveClaims { get; set; }
    public decimal TotalPremiumVolume { get; set; }
}

public class GetDashboardStatsHandler(BrokerSystemDbContext db, ICacheService cache, ICurrentUserService currentUserService) 
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsResponse>
{
    private string GetCacheKey() => currentUserService.IsAdmin 
        ? "DashboardStats_Admin" 
        : $"DashboardStats_Agent_{currentUserService.AgentId ?? 0}";

    /// <summary>
    /// Builds the multi-result SQL query for dashboard statistics, optionally filtered by agent.
    /// </summary>
    public static string GetDashboardStatsSql(ISqlDialect sqlDialect, int? agentId) 
    {
        var agentFilter = agentId.HasValue ? " AND agent_id = @AgentId" : "";
        var clientAgentFilter = agentId.HasValue 
            ? " WHERE EXISTS (SELECT 1 FROM policies p2 WHERE p2.client_id = c.client_id AND p2.agent_id = @AgentId)" 
            : "";

        return $@"
                -- Monthly Sales
                SELECT 
                    {sqlDialect.FormattedMonthYear("start_date")} as Month, 
                    COALESCE(SUM(premium_amount), 0) as TotalPremium, 
                    COUNT(*) as PolicyCount
                FROM policies
                WHERE start_date >= @StartDateLimit {agentFilter}
                GROUP BY {sqlDialect.Year("start_date")}, {sqlDialect.Month("start_date")}
                ORDER BY {sqlDialect.Year("start_date")}, {sqlDialect.Month("start_date")};

                -- Client Type Distribution
                SELECT ct.type_name as ClientType, COUNT(*) as ClientCount
                FROM clients c
                JOIN client_types ct ON c.client_type_id = ct.client_type_id
                {clientAgentFilter}
                GROUP BY ct.type_name
                ORDER BY ClientCount DESC;

                -- Policy Status Distribution
                SELECT ps.status_name as PolicyStatus, COUNT(*) as PolicyCount
                FROM policies p
                JOIN policy_statuses ps ON p.status_id = ps.status_id
                WHERE 1=1 {agentFilter}
                GROUP BY ps.status_name
                ORDER BY PolicyCount DESC;

                -- KPIs
                SELECT 
                    (SELECT COUNT(*) FROM clients c {clientAgentFilter}) as TotalClients,
                    (SELECT COUNT(*) FROM policies WHERE 1=1 {agentFilter}) as TotalPolicies,
                    (SELECT COUNT(*) FROM claims cl JOIN policies p ON cl.policy_id = p.policy_id WHERE 1=1 {agentFilter}) as ActiveClaims,
                    COALESCE((SELECT SUM(premium_amount) FROM policies WHERE 1=1 {agentFilter}), 0) as TotalPremiumVolume;
            ";
    }

    public async Task<DashboardStatsResponse> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(GetCacheKey(), async () =>
        {
            using var connection = db.Database.GetDbConnection();
            
            var sqlDialect = db.Database.Sql();
            var startDateLimit = CalculateStartDateLimit(DateTime.Today);
            var agentId = currentUserService.IsAdmin ? null : currentUserService.AgentId;

            var sql = GetDashboardStatsSql(sqlDialect, agentId);

            using var multi = await connection.QueryMultipleAsync(sql, new 
            { 
                StartDateLimit = startDateLimit,
                AgentId = agentId
            });

            var monthlySales = (await multi.ReadAsync<MonthlySales>()).ToList();
            var clientTypes = (await multi.ReadAsync<ClientTypeDistribution>()).ToList();
            var policyStatuses = (await multi.ReadAsync<PolicyStatusDistribution>()).ToList();
            var kpis = await multi.ReadSingleAsync<DashboardKpis>();

            return new DashboardStatsResponse
            {
                MonthlySales = monthlySales,
                ClientTypeDistribution = clientTypes,
                PolicyStatusDistribution = policyStatuses,
                Kpis = kpis
            };
        }, TimeSpan.FromMinutes(10));
    }

    public static DateTime CalculateStartDateLimit(DateTime today) => 
        new DateTime(today.Year, today.Month, 1).AddMonths(-11);
}


