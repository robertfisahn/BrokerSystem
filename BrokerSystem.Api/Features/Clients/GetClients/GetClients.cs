using BrokerSystem.Api.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Features.Clients.GetClients;

public record GetClientsQuery() : IRequest<List<GetClientsDto>>;

public record GetClientsDto
{
    public int ClientId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
    public string? ClientType { get; init; }
    public string? PrimaryContact { get; init; }
    public string? City { get; init; }
    public int ActivePoliciesCount { get; init; }
}

public class GetClientsHandler(BrokerSystemDbContext db) : IRequestHandler<GetClientsQuery, List<GetClientsDto>>
{
    public async Task<List<GetClientsDto>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        return await db.Clients
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
            .ToListAsync(cancellationToken);
    }
}
