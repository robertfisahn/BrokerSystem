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
            using var multi = await connection.QueryMultipleAsync("usp_GetDashboardStats", commandType: CommandType.StoredProcedure);

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
