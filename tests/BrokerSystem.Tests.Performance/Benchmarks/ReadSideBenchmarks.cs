using BenchmarkDotNet.Attributes;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Features.Clients.GetClientsStats;
using BrokerSystem.Api.Features.Policies.GetPolicyLookups;
using BrokerSystem.Api.Features.Policies.ExportPolicy;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BrokerSystem.Api.Common.Exceptions;

namespace BrokerSystem.Tests.Performance.Benchmarks;

[MemoryDiagnoser]
public class ReadSideBenchmarks
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
            ?? throw new NotFoundException("Connection string 'DefaultConnection' not found.");
        
        var options = new DbContextOptionsBuilder<BrokerSystemDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        _db = new BrokerSystemDbContext(options);
    }

    // ===========================================
    // 1. GetPolicyLookups - ROUNDTRIP REDUCTION
    // ===========================================
    
    [Benchmark(Baseline = true)]
    public async Task<PolicyLookupsResponse> Lookups_EF_3Step()
    {
        var clients = await _db.Clients
            .Where(c => c.IsActive)
            .OrderBy(c => c.LastName ?? c.CompanyName)
            .Select(c => new LookupDto(c.ClientId, 
                !string.IsNullOrWhiteSpace(c.CompanyName) ? c.CompanyName :
                !string.IsNullOrWhiteSpace(c.FirstName + c.LastName) ? ((c.FirstName ?? "") + " " + (c.LastName ?? "")).Trim() :
                $"Client #{c.ClientId}"))
            .ToListAsync();

        var policyTypes = await _db.PolicyTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.TypeName)
            .Select(t => new LookupDto(t.PolicyTypeId, t.TypeName))
            .ToListAsync();

        var agents = await _db.Agents
            .Where(a => a.IsActive)
            .OrderBy(a => a.LastName)
            .Select(a => new LookupDto(a.AgentId, 
                !string.IsNullOrWhiteSpace(a.FirstName + a.LastName) ? ((a.FirstName ?? "") + " " + (a.LastName ?? "")).Trim() :
                $"Agent #{a.AgentId}"))
            .ToListAsync();

        return new PolicyLookupsResponse(clients, policyTypes, agents);
    }

    [Benchmark]
    public async Task<PolicyLookupsResponse> Lookups_Dapper_QueryMultiple()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT client_id AS Id, first_name AS FirstName, last_name AS LastName, company_name AS CompanyName 
            FROM clients WHERE is_active = 1 ORDER BY last_name, company_name;

            SELECT policy_type_id AS Id, type_name AS Name 
            FROM policy_types WHERE is_active = 1 ORDER BY type_name;

            SELECT agent_id AS Id, first_name AS FirstName, last_name AS LastName 
            FROM agents WHERE is_active = 1 ORDER BY last_name;";

        using var multi = await connection.QueryMultipleAsync(sql);

        var rawClients = await multi.ReadAsync<dynamic>();
        var clients = rawClients.Select(c => new LookupDto(
            (int)c.Id,
            !string.IsNullOrWhiteSpace((string?)c.CompanyName) ? (string)c.CompanyName :
            !string.IsNullOrWhiteSpace((string?)c.FirstName + (string?)c.LastName) ? (((string?)c.FirstName ?? "") + " " + ((string?)c.LastName ?? "")).Trim() :
            $"Client #{c.Id}"
        )).ToList();

        var policyTypes = (await multi.ReadAsync<LookupDto>()).ToList();

        var rawAgents = await multi.ReadAsync<dynamic>();
        var agents = rawAgents.Select(a => new LookupDto(
            (int)a.Id,
            !string.IsNullOrWhiteSpace((string?)a.FirstName + (string?)a.LastName) ? (((string?)a.FirstName ?? "") + " " + ((string?)a.LastName ?? "")).Trim() :
            $"Agent #{a.Id}"
        )).ToList();

        return new PolicyLookupsResponse(clients, policyTypes, agents);
    }

    // ===========================================
    // 2. GetClientsStats - AGGREGATION SPEED
    // ===========================================

    [Benchmark]
    public async Task<ClientsStatsDto> Stats_EF_GroupBy()
    {
        var startOfMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);

        var stats = await _db.Clients
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
            .FirstOrDefaultAsync();

        return stats ?? new ClientsStatsDto();
    }

    [Benchmark]
    public async Task<ClientsStatsDto> Stats_Dapper_RawSql()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            DECLARE @StartOfMonth DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);

            SELECT 
                (SELECT COUNT(*) FROM clients) as TotalClients,
                (SELECT COUNT(*) FROM clients c JOIN client_types ct ON c.client_type_id = ct.client_type_id WHERE ct.type_name = 'VIP') as VipClients,
                (SELECT COUNT(*) FROM clients c JOIN client_types ct ON c.client_type_id = ct.client_type_id WHERE ct.type_name = 'Corporate') as CorporateClients,
                (SELECT COUNT(*) FROM policies p JOIN policy_statuses ps ON p.status_id = ps.status_id WHERE ps.is_active_policy = 1) as ActivePoliciesTotal,
                (SELECT COUNT(*) FROM clients WHERE registration_date >= @StartOfMonth) as NewClientsThisMonth";

        return await connection.QuerySingleOrDefaultAsync<ClientsStatsDto>(sql) ?? new ClientsStatsDto();
    }

    // ===========================================
    // 3. ExportPolicy - SINGLE READ OPTIMIZATION
    // ===========================================

    [Benchmark]
    public async Task<PolicyExportDto> Export_EF_Select()
    {
        return await _db.Policies
            .OrderBy(p => p.PolicyId)
            .Select(p => new PolicyExportDto(
                p.PolicyNumber,
                p.Client.FirstName ?? "",
                p.Client.LastName ?? "",
                p.Client.CompanyName,
                p.PolicyType.TypeName,
                p.SumInsured,
                p.PremiumAmount,
                p.StartDate.ToDateTime(TimeOnly.MinValue),
                p.EndDate.ToDateTime(TimeOnly.MinValue),
                p.Status.StatusName,
                (p.Agent.FirstName ?? "") + " " + (p.Agent.LastName ?? "")
            ))
            .FirstOrDefaultAsync() ?? throw new Exception();
    }

    [Benchmark]
    public async Task<PolicyExportDto> Export_Dapper_Query()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT TOP 1
                p.policy_number AS PolicyNumber,
                c.first_name AS ClientFirstName,
                c.last_name AS ClientLastName,
                c.company_name AS ClientCompanyName,
                pt.type_name AS PolicyTypeName,
                p.sum_insured AS SumInsured,
                p.premium_amount AS PremiumAmount,
                p.start_date AS StartDate,
                p.end_date AS EndDate,
                ps.status_name AS StatusName,
                (a.first_name + ' ' + a.last_name) AS AgentName
            FROM policies p
            JOIN clients c ON p.client_id = c.client_id
            JOIN policy_types pt ON p.policy_type_id = pt.policy_type_id
            JOIN policy_statuses ps ON p.status_id = ps.status_id
            JOIN agents a ON p.agent_id = a.agent_id
            ORDER BY p.policy_id";

        return await connection.QueryFirstOrDefaultAsync<PolicyExportDto>(sql) ?? throw new Exception();
    }
}
