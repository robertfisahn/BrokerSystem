using BenchmarkDotNet.Attributes;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Features.Dashboard;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace BrokerSystem.Tests.Performance.Benchmarks.Dashboard;

[MemoryDiagnoser]
public class DashboardBenchmarks
{
    private BrokerSystemDbContext _db = null!;
    private string _connectionString = null!;

    [GlobalSetup]
    public void Setup()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory)!.FullName)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        
        var options = new DbContextOptionsBuilder<BrokerSystemDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        _db = new BrokerSystemDbContext(options);
    }

    [Benchmark(Baseline = true)]
    public async Task<DashboardStatsResponse> Dashboard_Dapper_Procedure()
    {
        // 1. Aktualna implementacja z GetDashboardStats.cs (Dapper + Stored Procedure)
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var multi = await connection.QueryMultipleAsync("usp_GetDashboardStats", commandType: CommandType.StoredProcedure);

        var monthlySales = (await multi.ReadAsync<MonthlySales>()).ToList();
        var clientTypes = (await multi.ReadAsync<ClientTypeDistribution>()).ToList();
        var policyStatuses = (await multi.ReadAsync<PolicyStatusDistribution>()).ToList();
        var kpis = await multi.ReadSingleAsync<DashboardKpis>();

        return new DashboardStatsResponse(monthlySales, clientTypes, policyStatuses, kpis);
    }

    [Benchmark]
    public async Task<DashboardStatsResponse> Dashboard_EF_MultiQuery()
    {
        // 2. Symulacja implementacji EF Core (Kilka zapytań = Wiele Roundtripów)
        // Na localhost narzut sieciowy (latency) jest bliski zeru (<0.1ms).
        // W chmurze (Azure/AWS), gdzie ping wynosi np. 5ms, ten wariant będzie o 20-30ms wolniejszy 
        // od Dappera ze względu na 4 osobne strzały do bazy danych!
        
        var monthlySales = await _db.Policies
            .GroupBy(p => new { p.StartDate.Year, p.StartDate.Month })
            .Select(g => new MonthlySales(
                $"{g.Key.Year}-{g.Key.Month:D2}",
                g.Sum(p => p.PremiumAmount),
                g.Count()
            ))
            .ToListAsync();

        var clientTypes = await _db.Clients
            .GroupBy(c => c.ClientType.TypeName)
            .Select(g => new ClientTypeDistribution(g.Key, g.Count()))
            .ToListAsync();

        var policyStatuses = await _db.Policies
            .GroupBy(p => p.Status.StatusName)
            .Select(g => new PolicyStatusDistribution(g.Key, g.Count()))
            .ToListAsync();

        var kpis = new DashboardKpis(
            await _db.Clients.CountAsync(),
            await _db.Policies.CountAsync(),
            10,
            await _db.Policies.SumAsync(p => p.PremiumAmount)
        );

        return new DashboardStatsResponse(monthlySales, clientTypes, policyStatuses, kpis);
    }

    [Benchmark]
    public async Task<DashboardStatsResponse> Dashboard_Dapper_QueryMultiple_RawSql()
    {
        // 3. Dapper QueryMultiple + Raw SQL (1 Roundtrip, bez procedury)
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        const string sql = @"
            -- Monthly Sales (Szybszy sposób niż FORMAT)
            SELECT CONCAT(DATEPART(year, start_date), '-', RIGHT('0' + CAST(DATEPART(month, start_date) AS VARCHAR), 2)) as Month, 
                   SUM(premium_amount) as TotalPremium, COUNT(*) as PolicyCount
            FROM policies
            GROUP BY DATEPART(year, start_date), DATEPART(month, start_date);

            -- Client Type Distribution
            SELECT ct.type_name as ClientType, COUNT(*) as ClientCount
            FROM clients c
            JOIN client_types ct ON c.client_type_id = ct.client_type_id
            GROUP BY ct.type_name;

            -- Policy Status Distribution
            SELECT ps.status_name as PolicyStatus, COUNT(*) as PolicyCount
            FROM policies p
            JOIN policy_statuses ps ON p.status_id = ps.status_id
            GROUP BY ps.status_name;

            -- KPIs
            SELECT 
                (SELECT COUNT(*) FROM clients) as TotalClients,
                (SELECT COUNT(*) FROM policies) as TotalPolicies,
                10 as ActiveClaims,
                (SELECT SUM(premium_amount) FROM policies) as TotalPremiumVolume;
        ";

        using var multi = await connection.QueryMultipleAsync(sql);

        var monthlySales = (await multi.ReadAsync<MonthlySales>()).ToList();
        var clientTypes = (await multi.ReadAsync<ClientTypeDistribution>()).ToList();
        var policyStatuses = (await multi.ReadAsync<PolicyStatusDistribution>()).ToList();
        var kpis = await multi.ReadSingleAsync<DashboardKpis>();

        return new DashboardStatsResponse(monthlySales, clientTypes, policyStatuses, kpis);
    }
}
