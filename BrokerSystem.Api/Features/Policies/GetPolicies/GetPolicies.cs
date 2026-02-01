using BrokerSystem.Api.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace BrokerSystem.Api.Features.Policies.GetPolicies;

public record GetPoliciesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
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
    DateOnly StartDate,
    DateOnly EndDate,
    string Status);

public class GetPoliciesHandler(BrokerSystemDbContext db) : IRequestHandler<GetPoliciesQuery, PagedPoliciesResponse>
{
    public async Task<PagedPoliciesResponse> Handle(GetPoliciesQuery request, CancellationToken ct)
    {
        using var connection = db.Database.GetDbConnection();

        // 1. Dynamic whereClause (Build statement based on Search)
        var whereClause = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchWords = request.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < searchWords.Length; i++)
            {
                var pName = $"@p{i}";
                whereClause += $" AND (p.policy_number LIKE {pName} OR c.first_name LIKE {pName} OR c.last_name LIKE {pName})";
                parameters.Add(pName, $"%{searchWords[i]}%");
            }
        }

        // 2. Count Query
        var countSql = $@"
            SELECT COUNT(*) 
            FROM policies p
            INNER JOIN clients c ON p.client_id = c.client_id
            {whereClause}";
            
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // 3. Sorting logic
        var orderBy = request.SortBy?.ToLower() switch
        {
            "policynumber" => "p.policy_number",
            "clientname" => "c.last_name",
            "totalpremium" => "p.premium_amount",
            "status" => "ps.status_name",
            _ => "p.created_at"
        };
        orderBy += request.SortDescending ? " DESC" : " ASC";

        // 4. Data Query (Dapper Optimized)
        var sql = $@"
            SELECT 
                p.policy_id AS PolicyId,
                p.policy_number AS PolicyNumber,
                c.first_name + ' ' + c.last_name AS ClientName,
                pt.type_name AS PolicyType,
                p.premium_amount AS TotalPremium,
                p.start_date AS StartDate,
                p.end_date AS EndDate,
                ps.status_name AS Status
            FROM policies p
            INNER JOIN clients c ON p.client_id = c.client_id
            INNER JOIN policy_types pt ON p.policy_type_id = pt.policy_type_id
            INNER JOIN policy_statuses ps ON p.status_id = ps.status_id
            {whereClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        parameters.Add("@Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("@PageSize", request.PageSize);

        var items = (await connection.QueryAsync<PolicyDto>(sql, parameters)).ToList();

        return new PagedPoliciesResponse(items, totalCount, request.Page, request.PageSize);
    }
}
