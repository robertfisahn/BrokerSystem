using BrokerSystem.Api.Common.Exceptions;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Dapper;
using System.Data;

namespace BrokerSystem.Api.Features.Clients.GetClient360;

/// <summary>
/// Query to retrieve a comprehensive 360-degree view of a client, including contacts, addresses, policies, and claims.
/// </summary>
public record GetClient360Query(int ClientId) : IRequest<Client360Dto?>;

public class GetClient360Validator : AbstractValidator<GetClient360Query>
{
    public GetClient360Validator()
    {
        RuleFor(x => x.ClientId).GreaterThan(0).WithMessage("ClientId must be greater than 0.");
    }
}

public record Client360Dto
{
    public int ClientId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
    public string? TaxId { get; init; }
    public DateOnly RegistrationDate { get; init; }
    public string? ClientType { get; init; }
    
    public List<Client360ContactDto> Contacts { get; init; } = [];
    public List<Client360AddressDto> Addresses { get; init; } = [];
    public List<Client360PolicyDto> Policies { get; init; } = [];
}

public record Client360ContactDto
{
    public string? ContactType { get; init; }
    public string? ContactValue { get; init; }
    public bool IsPrimary { get; init; }
}

public record Client360AddressDto
{
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public bool IsCurrent { get; init; }
}

public record Client360PolicyDto
{
    public int PolicyId { get; init; }
    public string? PolicyNumber { get; init; }
    public string? PolicyType { get; init; }
    public string? Status { get; init; }
    public decimal PremiumAmount { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public List<Client360ClaimDto> Claims { get; init; } = [];
}

public record Client360ClaimDto
{
    public int ClaimId { get; init; }
    public string? ClaimNumber { get; init; }
    public string? Status { get; init; }
    public decimal? ApprovedAmount { get; init; }
    public DateOnly IncidentDate { get; init; }
}

public class GetClient360Handler(BrokerSystemDbContext db) : IRequestHandler<GetClient360Query, Client360Dto?>
{
    public async Task<Client360Dto?> Handle(GetClient360Query request, CancellationToken cancellationToken)
    {
        using var connection = db.Database.GetDbConnection();

        using var multi = await connection.QueryMultipleAsync(GetMainSql, new { Id = request.ClientId });
        
        var client = await multi.ReadFirstOrDefaultAsync<Client360Dto>();
        if (client == null)
        {
            throw new NotFoundException($"Klient o ID {request.ClientId} nie został znaleziony.");
        }

        var contacts = await multi.ReadAsync<Client360ContactDto>();
        var addresses = await multi.ReadAsync<Client360AddressDto>();
        var policies = await multi.ReadAsync<Client360PolicyDtoInternal>();
        var claims = await multi.ReadAsync<Client360ClaimDtoInternal>();

        return MapToClient360Dto(client, contacts, addresses, policies, claims);
    }

    public const string GetMainSql = @"
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

    /// <summary>
    /// Maps raw database results into a structured Client360Dto model, linking policies with their related claims.
    /// </summary>
    public static Client360Dto MapToClient360Dto(
        Client360Dto client,
        IEnumerable<Client360ContactDto> contacts,
        IEnumerable<Client360AddressDto> addresses,
        IEnumerable<Client360PolicyDtoInternal> policies,
        IEnumerable<Client360ClaimDtoInternal> claims)
    {
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
            Claims = claimsLookup[p.PolicyId].Select(c => new Client360ClaimDto 
            { 
                ClaimId = c.ClaimId, 
                ClaimNumber = c.ClaimNumber, 
                Status = c.Status, 
                ApprovedAmount = c.ApprovedAmount, 
                IncidentDate = c.IncidentDate 
            }).ToList()
        }).ToList();

        return client with 
        { 
            Contacts = contacts.OrderByDescending(c => c.IsPrimary).ToList(), 
            Addresses = addresses.OrderByDescending(a => a.IsCurrent).ToList(), 
            Policies = finalPolicies 
        };
    }

    public record Client360PolicyDtoInternal : Client360PolicyDto { public new int PolicyId { get; init; } }
    public record Client360ClaimDtoInternal : Client360ClaimDto { public int PolicyId { get; init; } }
}
