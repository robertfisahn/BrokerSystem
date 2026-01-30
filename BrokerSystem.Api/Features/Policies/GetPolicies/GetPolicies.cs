using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Features.Policies.GetPolicies;

public record GetPoliciesQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<PagedPoliciesResponse>;

public record PagedPoliciesResponse(
    List<PolicyDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record PolicyDto(
    int PolicyId,
    string PolicyNumber,
    string ClientName,
    string PolicyType,
    decimal TotalPremium,
    DateTime StartDate,
    DateTime EndDate,
    string Status);

public class GetPoliciesHandler(BrokerSystemDbContext db) : IRequestHandler<GetPoliciesQuery, PagedPoliciesResponse>
{
    public async Task<PagedPoliciesResponse> Handle(GetPoliciesQuery request, CancellationToken ct)
    {
        var query = db.Policies.AsNoTracking();

        // Filtrowanie
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p => 
                p.PolicyNumber.Contains(request.SearchTerm) ||
                (p.Client.LastName != null && p.Client.LastName.Contains(request.SearchTerm)) ||
                (p.Client.FirstName != null && p.Client.FirstName.Contains(request.SearchTerm)));
        }

        var totalCount = await query.CountAsync(ct);

        // Sortowanie i Pagynacja
        var itemsQuery = ApplySorting(query, request.SortBy, request.SortDescending);

        var items = await itemsQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PolicyDto(
                p.PolicyId,
                p.PolicyNumber,
                p.Client.FirstName + " " + p.Client.LastName,
                p.PolicyType.TypeName,
                p.PremiumAmount,
                p.StartDate.ToDateTime(TimeOnly.MinValue),
                p.EndDate.ToDateTime(TimeOnly.MinValue),
                p.Status.StatusName
            ))
            .ToListAsync(ct);

        return new PagedPoliciesResponse(items, totalCount, request.Page, request.PageSize);
    }

    private static IQueryable<Policy> ApplySorting(IQueryable<Policy> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLower() switch
        {
            "policynumber" => descending ? query.OrderByDescending(p => p.PolicyNumber) : query.OrderBy(p => p.PolicyNumber),
            "clientname" => descending ? query.OrderByDescending(p => p.Client.LastName) : query.OrderBy(p => p.Client.LastName),
            "totalpremium" => descending ? query.OrderByDescending(p => p.PremiumAmount) : query.OrderBy(p => p.PremiumAmount),
            "status" => descending ? query.OrderByDescending(p => p.Status.StatusName) : query.OrderBy(p => p.Status.StatusName),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };
    }
}
