using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Dapper;

namespace BrokerSystem.Api.Features.Policies.GetPolicies;

/// <summary>
/// Query to retrieve a paginated, filtered, and sorted list of policies.
/// </summary>
public record GetPoliciesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<PagedPoliciesResponse>;

public class GetPoliciesValidator : AbstractValidator<GetPoliciesQuery>
{
    public GetPoliciesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be at least 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}

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
        var sqlDialect = db.Database.Sql();

        // 1. Logic Isolation: Build statement based on Search
        var (whereClause, parameters) = BuildFilterQuery(request.Search);

        // 2. Count Query
        var countSql = $@"
            SELECT COUNT(*) 
            FROM policies p
            INNER JOIN clients c ON p.client_id = c.client_id
            {whereClause}";
            
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // 3. Logic Isolation: Sorting logic
        var orderBy = GetOrderBy(request.SortBy, request.SortDescending);

        // 4. Data Query
        var sql = GetMainSql(sqlDialect, whereClause, orderBy);

        parameters.Add("@Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("@PageSize", request.PageSize);

        var items = (await connection.QueryAsync<PolicyDto>(sql, parameters)).ToList();

        return new PagedPoliciesResponse(items, totalCount, request.Page, request.PageSize);
    }

    /// <summary>
    /// Builds the SQL WHERE clause for policy filtering based on a search term.
    /// </summary>
    public static (string WhereClause, DynamicParameters Parameters) BuildFilterQuery(string? search)
    {
        var whereClause = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (string.IsNullOrWhiteSpace(search))
        {
            return (whereClause, parameters);
        }

        var searchWords = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < searchWords.Length; i++)
        {
            var pName = $"@p{i}";
            whereClause += $" AND (p.policy_number LIKE {pName} OR c.first_name LIKE {pName} OR c.last_name LIKE {pName})";
            parameters.Add(pName, $"%{searchWords[i]}%");
        }

        return (whereClause, parameters);
    }

    /// <summary>
    /// Returns the SQL ORDER BY fragment for policy sorting.
    /// </summary>
    public static string GetOrderBy(string? sortBy, bool sortDescending)
    {
        var orderBy = sortBy?.ToLower() switch
        {
            "policynumber" => "p.policy_number",
            "clientname" => "c.last_name",
            "totalpremium" => "p.premium_amount",
            "status" => "ps.status_name",
            _ => "p.created_at"
        };
        
        return orderBy + (sortDescending ? " DESC" : " ASC");
    }

    /// <summary>
    /// Returns the main SQL query for fetching policy data, including joins for clients, types, and statuses.
    /// </summary>
    public static string GetMainSql(ISqlDialect sqlDialect, string whereClause, string orderBy) => $@"
            SELECT 
                p.policy_id AS PolicyId,
                p.policy_number AS PolicyNumber,
                {sqlDialect.Concat("c.first_name", "' '", "c.last_name")} AS ClientName,
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
            {sqlDialect.Paging("@Offset", "@PageSize")}";
}
