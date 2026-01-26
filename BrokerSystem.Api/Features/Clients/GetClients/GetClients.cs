using BrokerSystem.Api.Common.Models;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Features.Clients.GetClients;

public record GetClientsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string SortBy = "clientId",
    bool SortDescending = false
) : IRequest<PaginatedResult<GetClientsDto>>;

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

public class GetClientsHandler(BrokerSystemDbContext db) : IRequestHandler<GetClientsQuery, PaginatedResult<GetClientsDto>>
{
    public async Task<PaginatedResult<GetClientsDto>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Clients.AsNoTracking().AsQueryable();

        // Search filter (Multi-word support)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchWords = request.Search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in searchWords)
            {
                query = query.Where(c =>
                    (c.FirstName != null && c.FirstName.ToLower().Contains(word)) ||
                    (c.LastName != null && c.LastName.ToLower().Contains(word)) ||
                    (c.CompanyName != null && c.CompanyName.ToLower().Contains(word))
                );
            }
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = request.SortBy.ToLower() switch
        {
            "firstname" => request.SortDescending 
                ? query.OrderByDescending(c => c.FirstName) 
                : query.OrderBy(c => c.FirstName),
            "lastname" => request.SortDescending 
                ? query.OrderByDescending(c => c.LastName) 
                : query.OrderBy(c => c.LastName),
            "companyname" => request.SortDescending 
                ? query.OrderByDescending(c => c.CompanyName) 
                : query.OrderBy(c => c.CompanyName),
            "clienttype" => request.SortDescending 
                ? query.OrderByDescending(c => c.ClientType.TypeName) 
                : query.OrderBy(c => c.ClientType.TypeName),
            _ => request.SortDescending 
                ? query.OrderByDescending(c => c.ClientId) 
                : query.OrderBy(c => c.ClientId)
        };

        // Pagination
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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

        return new PaginatedResult<GetClientsDto>(items, totalCount, request.Page, request.PageSize);
    }
}
