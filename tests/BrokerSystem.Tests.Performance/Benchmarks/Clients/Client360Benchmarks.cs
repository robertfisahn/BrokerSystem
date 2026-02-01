using BenchmarkDotNet.Attributes;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Features.Clients.GetClient360;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace BrokerSystem.Tests.Performance.Benchmarks.Clients;

[MemoryDiagnoser]
public class Client360Benchmarks
{
    private BrokerSystemDbContext _db = null!;
    private string _connectionString = null!;
    private int _testClientId = 1;

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

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        
        var client = _db.Clients.FirstOrDefault();
        if (client != null) _testClientId = client.ClientId;
    }

    [Benchmark(Baseline = true)]
    public async Task<Client360Dto?> Client360_EF_Current()
    {
        // 1. Aktualna implementacja z GetClient360.cs (EF Core + .Select)
        return await _db.Clients
            .AsNoTracking()
            .Where(c => c.ClientId == _testClientId)
            .Select(c => new Client360Dto
            {
                ClientId = c.ClientId,
                FirstName = c.FirstName,
                LastName = c.LastName,
                CompanyName = c.CompanyName,
                TaxId = c.TaxId,
                RegistrationDate = c.RegistrationDate,
                ClientType = c.ClientType.TypeName,
                Contacts = c.ClientContacts.Select(ct => new Client360ContactDto { ContactType = ct.ContactType, ContactValue = ct.ContactValue, IsPrimary = ct.IsPrimary }).ToList(),
                Addresses = c.ClientAddresses.Select(a => new Client360AddressDto { Street = a.Street, City = a.City, PostalCode = a.PostalCode, Country = a.Country, IsCurrent = a.IsCurrent }).ToList(),
                Policies = c.Policies.Select(p => new Client360PolicyDto
                {
                    PolicyId = p.PolicyId,
                    PolicyNumber = p.PolicyNumber,
                    PolicyType = p.PolicyType.TypeName,
                    Status = p.Status.StatusName,
                    PremiumAmount = p.PremiumAmount,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Claims = p.Claims.Select(cl => new Client360ClaimDto { ClaimId = cl.ClaimId, ClaimNumber = cl.ClaimNumber, Status = cl.Status.StatusName, ApprovedAmount = cl.ApprovedAmount, IncidentDate = cl.IncidentDate }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    [Benchmark]
    public async Task<Client360Dto?> Client360_EF_Heavy_Includes()
    {
        // 2. Pobranie pełnych encji (Include) i mapowanie ręczne
        var client = await _db.Clients
            .AsNoTracking()
            .Include(c => c.ClientType)
            .Include(c => c.ClientContacts)
            .Include(c => c.ClientAddresses)
            .Include(c => c.Policies)
                .ThenInclude(p => p.Status)
            .Include(c => c.Policies)
                .ThenInclude(p => p.PolicyType)
            .Include(c => c.Policies)
                .ThenInclude(p => p.Claims)
                    .ThenInclude(cl => cl.Status)
            .FirstOrDefaultAsync(c => c.ClientId == _testClientId);

        if (client == null) return null;

        return new Client360Dto
        {
            ClientId = client.ClientId,
            FirstName = client.FirstName,
            LastName = client.LastName,
            CompanyName = client.CompanyName,
            TaxId = client.TaxId,
            RegistrationDate = client.RegistrationDate,
            ClientType = client.ClientType?.TypeName,
            Contacts = client.ClientContacts.Select(ct => new Client360ContactDto { ContactType = ct.ContactType, ContactValue = ct.ContactValue, IsPrimary = ct.IsPrimary }).ToList(),
            Addresses = client.ClientAddresses.Select(a => new Client360AddressDto { Street = a.Street, City = a.City, PostalCode = a.PostalCode, Country = a.Country, IsCurrent = a.IsCurrent }).ToList(),
            Policies = client.Policies.Select(p => new Client360PolicyDto
            {
                PolicyId = p.PolicyId,
                PolicyNumber = p.PolicyNumber,
                PolicyType = p.PolicyType?.TypeName,
                Status = p.Status?.StatusName,
                PremiumAmount = p.PremiumAmount,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Claims = p.Claims.Select(cl => new Client360ClaimDto { ClaimId = cl.ClaimId, ClaimNumber = cl.ClaimNumber, Status = cl.Status?.StatusName, ApprovedAmount = cl.ApprovedAmount, IncidentDate = cl.IncidentDate }).ToList()
            }).ToList()
        };
    }

    [Benchmark]
    public async Task<Client360Dto?> Client360_Dapper_QueryMultiple_Hybrid()
    {
        // 3. Dapper QueryMultiple (pobranie list w jednym strzale bez JOINów)
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT c.client_id AS ClientId, c.first_name AS FirstName, c.last_name AS LastName, 
                   c.company_name AS CompanyName, c.tax_id AS TaxId, c.registration_date AS RegistrationDate,
                   ct.type_name AS ClientType
            FROM clients c
            JOIN client_types ct ON c.client_type_id = ct.client_type_id
            WHERE c.client_id = @Id;

            SELECT contact_type AS ContactType, contact_value AS ContactValue, is_primary AS IsPrimary
            FROM client_contacts
            WHERE client_id = @Id;

            SELECT street AS Street, city AS City, postal_code AS PostalCode, country AS Country, is_current AS IsCurrent
            FROM client_addresses
            WHERE client_id = @Id;

            SELECT p.policy_id AS PolicyId, p.policy_number AS PolicyNumber, pt.type_name AS PolicyType, 
                   ps.status_name AS Status, p.premium_amount AS PremiumAmount, p.start_date AS StartDate, p.end_date AS EndDate
            FROM policies p
            JOIN policy_types pt ON p.policy_type_id = pt.policy_type_id
            JOIN policy_statuses ps ON p.status_id = ps.status_id
            WHERE p.client_id = @Id;

            SELECT cl.claim_id AS ClaimId, cl.claim_number AS ClaimNumber, cs.status_name AS Status, 
                   cl.approved_amount AS ApprovedAmount, cl.incident_date AS IncidentDate, cl.policy_id AS PolicyId
            FROM claims cl
            JOIN claim_statuses cs ON cl.status_id = cs.status_id
            WHERE cl.policy_id IN (SELECT policy_id FROM policies WHERE client_id = @Id);
        ";

        using var multi = await connection.QueryMultipleAsync(sql, new { Id = _testClientId });
        var client = await multi.ReadFirstOrDefaultAsync<Client360Dto>();
        if (client == null) return null;

        var contacts = await multi.ReadAsync<Client360ContactDto>();
        var addresses = await multi.ReadAsync<Client360AddressDto>();
        var policies = await multi.ReadAsync<Client360PolicyDtoInternal>();
        var claims = await multi.ReadAsync<Client360ClaimDtoInternal>();

        var claimsLookup = claims.ToLookup(c => c.PolicyId);
        var finalPolicies = policies.Select(p => new Client360PolicyDto
        {
            PolicyId = p.PolicyId,
            PolicyNumber = p.PolicyNumber,
            PolicyType = p.PolicyType,
            Status = p.Status,
            PremiumAmount = p.PremiumAmount,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Claims = claimsLookup[p.PolicyId].Select(c => new Client360ClaimDto { ClaimId = c.ClaimId, ClaimNumber = c.ClaimNumber, Status = c.Status, ApprovedAmount = c.ApprovedAmount, IncidentDate = c.IncidentDate }).ToList()
        }).ToList();

        return client with { Contacts = contacts.ToList(), Addresses = addresses.ToList(), Policies = finalPolicies };
    }

    private record Client360PolicyDtoInternal : Client360PolicyDto { public int PolicyId { get; init; } }
    private record Client360ClaimDtoInternal : Client360ClaimDto { public int PolicyId { get; init; } }
    
    private class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly> 
    { 
        public override void SetValue(IDbDataParameter p, DateOnly v) { p.Value = v.ToDateTime(TimeOnly.MinValue); p.DbType = DbType.Date; } 
        public override DateOnly Parse(object v) => DateOnly.FromDateTime((DateTime)v); 
    }
}
