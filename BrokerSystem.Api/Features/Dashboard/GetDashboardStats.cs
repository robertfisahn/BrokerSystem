using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Common.Caching;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Dapper;

namespace BrokerSystem.Api.Features.Dashboard;

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

    public async Task<DashboardStatsResponse> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(CacheKey, async () =>
        {
            Console.WriteLine($"[DB HIT] {DateTime.Now:HH:mm:ss}");
            using var connection = db.Database.GetDbConnection();
            
            const string sql = @"
                -- Monthly Sales (Zoptymalizowany, ostatnie 12 miesięcy, poprawna kolejność)
                WITH MonthlySums AS (
                    SELECT 
                        YEAR(start_date) as Yr, 
                        MONTH(start_date) as Mo, 
                        ISNULL(SUM(premium_amount), 0) as TotalPremium, 
                        COUNT(*) as PolicyCount
                    FROM policies
                    WHERE start_date >= DATEADD(month, -11, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                    GROUP BY YEAR(start_date), MONTH(start_date)
                )
                SELECT 
                    CONCAT(Yr, '-', RIGHT('0' + CAST(Mo AS VARCHAR), 2)) as Month, 
                    TotalPremium, 
                    PolicyCount
                FROM MonthlySums
                ORDER BY Yr, Mo;

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
                    ISNULL((SELECT SUM(premium_amount) FROM policies), 0) as TotalPremiumVolume;
            ";

            using var multi = await connection.QueryMultipleAsync(sql);

            var monthlySales = (await multi.ReadAsync<MonthlySales>()).ToList();
            var clientTypes = (await multi.ReadAsync<ClientTypeDistribution>()).ToList();
            var policyStatuses = (await multi.ReadAsync<PolicyStatusDistribution>()).ToList();
            var kpis = await multi.ReadSingleAsync<DashboardKpis>();

            return new DashboardStatsResponse(monthlySales, clientTypes, policyStatuses, kpis);
        }, TimeSpan.FromMinutes(10));
    }
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
