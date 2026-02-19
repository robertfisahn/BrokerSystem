using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Common.Caching;
using BrokerSystem.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace BrokerSystem.Api.Features.Dashboard;

/// <summary>
/// Query to retrieve aggregated dashboard statistics, including sales trends and distribution charts.
/// </summary>
public record GetDashboardStatsQuery() : IRequest<DashboardStatsResponse>;

public record DashboardStatsResponse(
    List<MonthlySales> MonthlySales,
    List<ClientTypeDistribution> ClientTypeDistribution,
    List<PolicyStatusDistribution> PolicyStatusDistribution,
    DashboardKpis Kpis
);

public record MonthlySales(string Month, decimal TotalPremium, int PolicyCount);

public record ClientTypeDistribution(string ClientType, int ClientCount);

public record PolicyStatusDistribution(string PolicyStatus, int PolicyCount);

public record DashboardKpis(
    int TotalClients,
    int TotalPolicies,
    int ActiveClaims,
    decimal TotalPremiumVolume
);

public class GetDashboardStatsHandler(BrokerSystemDbContext db, ICacheService cache) : IRequestHandler<GetDashboardStatsQuery, DashboardStatsResponse>
{
    private const string CacheKey = "DashboardStats";

    /// <summary>
    /// Builds the multi-result SQL query for dashboard statistics.
    /// </summary>
    public static string GetDashboardStatsSql(ISqlDialect sqlDialect) => $@"
                -- Monthly Sales (Cross-platform version)
                SELECT 
                    {sqlDialect.FormattedMonthYear("start_date")} as Month, 
                    COALESCE(SUM(premium_amount), 0) as TotalPremium, 
                    COUNT(*) as PolicyCount
                FROM policies
                WHERE start_date >= @StartDateLimit
                GROUP BY {sqlDialect.Year("start_date")}, {sqlDialect.Month("start_date")}
                ORDER BY {sqlDialect.Year("start_date")}, {sqlDialect.Month("start_date")};

                -- Client Type Distribution
                SELECT ct.type_name as ClientType, COUNT(*) as ClientCount
                FROM clients c
                JOIN client_types ct ON c.client_type_id = ct.client_type_id
                GROUP BY ct.type_name
                ORDER BY ClientCount DESC;

                -- Policy Status Distribution
                SELECT ps.status_name as PolicyStatus, COUNT(*) as PolicyCount
                FROM policies p
                JOIN policy_statuses ps ON p.status_id = ps.status_id
                GROUP BY ps.status_name
                ORDER BY PolicyCount DESC;

                -- KPIs (1 Roundtrip)
                SELECT 
                    (SELECT COUNT(*) FROM clients) as TotalClients,
                    (SELECT COUNT(*) FROM policies) as TotalPolicies,
                    10 as ActiveClaims,
                    COALESCE((SELECT SUM(premium_amount) FROM policies), 0) as TotalPremiumVolume;
            ";

    public async Task<DashboardStatsResponse> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(CacheKey, async () =>
        {
            using var connection = db.Database.GetDbConnection();
            
            var sqlDialect = db.Database.Sql();
            var startDateLimit = CalculateStartDateLimit(DateTime.Today);

            var sql = GetDashboardStatsSql(sqlDialect);

            using var multi = await connection.QueryMultipleAsync(sql, new { StartDateLimit = startDateLimit });

            var monthlySales = (await multi.ReadAsync<MonthlySales>()).ToList();
            var clientTypes = (await multi.ReadAsync<ClientTypeDistribution>()).ToList();
            var policyStatuses = (await multi.ReadAsync<PolicyStatusDistribution>()).ToList();
            var kpis = await multi.ReadSingleAsync<DashboardKpis>();

            return new DashboardStatsResponse(monthlySales, clientTypes, policyStatuses, kpis);
        }, TimeSpan.FromMinutes(10));
    }

    /// <summary>
    /// Pure logic to calculate the start date for a 12-month trailing window.
    /// </summary>
    public static DateTime CalculateStartDateLimit(DateTime today) => 
        new DateTime(today.Year, today.Month, 1).AddMonths(-11);
}

[ApiController]
[Route("api/dashboard")]
public class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDashboardStatsQuery(), ct);
        return Ok(result);
    }
}
