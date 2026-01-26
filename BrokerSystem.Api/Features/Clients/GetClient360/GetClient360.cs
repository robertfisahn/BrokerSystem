using BrokerSystem.Api.Common.Exceptions;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Features.Clients.GetClient360;

public record GetClient360Query(int ClientId) : IRequest<Client360Dto?>;

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
        var result = await db.Clients
            .AsNoTracking()
            .Include(c => c.ClientContacts)
            .Include(c => c.ClientAddresses)
            .Include(c => c.Policies)
                .ThenInclude(p => p.Status)
            .Include(c => c.Policies)
                .ThenInclude(p => p.PolicyType)
            .Include(c => c.Policies)
                .ThenInclude(p => p.Claims)
                    .ThenInclude(cl => cl.Status)
            .Where(c => c.ClientId == request.ClientId)
            .Select(c => new Client360Dto
            {
                ClientId = c.ClientId,
                FirstName = c.FirstName,
                LastName = c.LastName,
                CompanyName = c.CompanyName,
                TaxId = c.TaxId,
                RegistrationDate = c.RegistrationDate,
                ClientType = c.ClientType.TypeName,
                Contacts = c.ClientContacts.Select(ct => new Client360ContactDto
                {
                    ContactType = ct.ContactType,
                    ContactValue = ct.ContactValue,
                    IsPrimary = ct.IsPrimary
                }).ToList(),
                Addresses = c.ClientAddresses.Select(a => new Client360AddressDto
                {
                    Street = a.Street,
                    City = a.City,
                    PostalCode = a.PostalCode,
                    Country = a.Country,
                    IsCurrent = a.IsCurrent
                }).ToList(),
                Policies = c.Policies.Select(p => new Client360PolicyDto
                {
                    PolicyId = p.PolicyId,
                    PolicyNumber = p.PolicyNumber,
                    PolicyType = p.PolicyType.TypeName,
                    Status = p.Status.StatusName,
                    PremiumAmount = p.PremiumAmount,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Claims = p.Claims.Select(cl => new Client360ClaimDto
                    {
                        ClaimId = cl.ClaimId,
                        ClaimNumber = cl.ClaimNumber,
                        Status = cl.Status.StatusName,
                        ApprovedAmount = cl.ApprovedAmount,
                        IncidentDate = cl.IncidentDate
                    }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            throw new NotFoundException($"Klient o ID {request.ClientId} nie został znaleziony.");
        }

        return result;
    }
}
