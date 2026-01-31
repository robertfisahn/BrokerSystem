using BenchmarkDotNet.Attributes;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Features.Clients.GetClients;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using BrokerSystem.Api.Common.Exceptions;

namespace BrokerSystem.Tests.Performance.Benchmarks.Clients;

[MemoryDiagnoser]
public class GetClientsBenchmarks
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

    [Benchmark(Baseline = true)]
    public async Task<List<GetClientsDto>> Listing_EF_Current()
    {
        // Aktualna implementacja z GetClients.cs
        // AsNoTracking() jest tu opcjonalne, ponieważ rzutowanie na DTO 
        // automatycznie wyłącza śledzenie zmian (Tracking). Pozostawiono dla jawności.
        return await _db.Clients
            .AsNoTracking()
            .Take(20)
            .Select(c => new GetClientsDto
            {
                ClientId = c.ClientId,
                FirstName = c.FirstName,
                LastName = c.LastName,
                CompanyName = c.CompanyName,
                ClientType = c.ClientType.TypeName,
                PrimaryContact = c.ClientContacts
                    .Where(ct => ct.IsPrimary)
                    .Select(ct => ct.ContactValue)
                    .FirstOrDefault(),
                City = c.ClientAddresses
                    .Where(a => a.IsCurrent)
                    .Select(a => a.City)
                    .FirstOrDefault(),
                ActivePoliciesCount = c.Policies.Count(p => p.Status.IsActivePolicy)
            })
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<GetClientsDto>> Listing_Dapper_RawSql()
    {
        // Sposób 2: Surowy SQL (Dapper) - pominięcie translatora LINQ
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT TOP 20
                c.client_id AS ClientId,
                c.first_name AS FirstName,
                c.last_name AS LastName,
                c.company_name AS CompanyName,
                ct.type_name AS ClientType,
                (SELECT TOP 1 cc.contact_value FROM client_contacts cc WHERE cc.client_id = c.client_id AND cc.is_primary = 1) AS PrimaryContact,
                (SELECT TOP 1 ca.city FROM client_addresses ca WHERE ca.client_id = c.client_id AND ca.is_current = 1) AS City,
                (SELECT COUNT(*) FROM policies p JOIN policy_statuses ps ON p.status_id = ps.status_id WHERE p.client_id = c.client_id AND ps.is_active_policy = 1) AS ActivePoliciesCount
            FROM clients c
            JOIN client_types ct ON c.client_type_id = ct.client_type_id";

        var result = await connection.QueryAsync<GetClientsDto>(sql);
        return result.ToList();
    }

    [Benchmark]
    public async Task<List<GetClientsDto>> Listing_EF_Heavy_Includes()
    {
        // Sposób 3: Pobranie pełnych encji (Include) i mapowanie w pamięci RAM.
        var items = await _db.Clients
            .AsNoTracking()
            .Include(c => c.ClientType)
            .Include(c => c.ClientContacts)
            .Include(c => c.ClientAddresses)
            .Include(c => c.Policies)
                .ThenInclude(p => p.Status)
            .Take(20)
            .ToListAsync();

        return items.Select(c => new GetClientsDto
        {
            ClientId = c.ClientId,
            FirstName = c.FirstName,
            LastName = c.LastName,
            CompanyName = c.CompanyName,
            ClientType = c.ClientType.TypeName,
            PrimaryContact = c.ClientContacts.FirstOrDefault(ct => ct.IsPrimary)?.ContactValue,
            City = c.ClientAddresses.FirstOrDefault(a => a.IsCurrent)?.City,
            ActivePoliciesCount = c.Policies.Count(p => p.Status.IsActivePolicy)
        }).ToList();
    }
    [Benchmark]
    public async Task<List<GetClientsDto>> Listing_EF_CompiledQuery()
    {
        // Sposób 4: Compiled Query - cache'owanie planu wykonania zapytania w EF Core.
        // Uniknięcie ponownej analizy drzewa wyrażeń LINQ
        var results = new List<GetClientsDto>();
        await foreach (var client in _compiledQuery(_db, 20))
        {
            results.Add(client);
        }
        return results;
    }

    private static readonly Func<BrokerSystemDbContext, int, IAsyncEnumerable<GetClientsDto>> _compiledQuery =
        EF.CompileAsyncQuery((BrokerSystemDbContext db, int top) =>
            db.Clients
                .AsNoTracking()
                .Take(top)
                .Select(c => new GetClientsDto
                {
                    ClientId = c.ClientId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    CompanyName = c.CompanyName,
                    ClientType = c.ClientType.TypeName,
                    PrimaryContact = c.ClientContacts
                        .Where(ct => ct.IsPrimary)
                        .Select(ct => ct.ContactValue)
                        .FirstOrDefault(),
                    City = c.ClientAddresses
                        .Where(a => a.IsCurrent)
                        .Select(a => a.City)
                        .FirstOrDefault(),
                    ActivePoliciesCount = c.Policies.Count(p => p.Status.IsActivePolicy)
                }));
}
