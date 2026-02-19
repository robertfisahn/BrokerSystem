using BrokerSystem.Api.Common.Models;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace BrokerSystem.Api.Features.Clients.GetClients;

/// <summary>
/// Query to retrieve a paginated, filtered, and sorted list of clients.
/// </summary>
public record GetClientsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string SortBy = "clientId",
    bool SortDescending = false
) : IRequest<PaginatedResult<GetClientsDto>>;

public class GetClientsValidator : AbstractValidator<GetClientsQuery>
{
    public GetClientsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be at least 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
        RuleFor(x => x.SortBy).NotEmpty().WithMessage("SortBy is required.");
    }
}

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
        using var connection = db.Database.GetDbConnection();
        var sqlDialect = db.Database.Sql();

        var (whereClause, parameters) = BuildFilterQuery(request.Search);
        var orderBy = GetOrderBy(request.SortBy, request.SortDescending);

        // 2. Count Query
        var countSql = $"SELECT COUNT(*) FROM clients c {whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // 4. Data Query
        var sql = GetMainSql(sqlDialect, whereClause, orderBy);

        parameters.Add("@Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("@PageSize", request.PageSize);

        var items = (await connection.QueryAsync<GetClientsDto>(sql, parameters)).ToList();

        return new PaginatedResult<GetClientsDto>(items, totalCount, request.Page, request.PageSize);
    }

    /// <summary>
    /// Builds a dynamic SQL WHERE clause for client filtering, searching across names, addresses, and contacts.
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
            whereClause += $" AND (c.first_name LIKE {pName} OR c.last_name LIKE {pName} OR c.company_name LIKE {pName} OR EXISTS (SELECT 1 FROM client_addresses ca WHERE ca.client_id = c.client_id AND ca.city LIKE {pName}) OR EXISTS (SELECT 1 FROM client_contacts cc WHERE cc.client_id = c.client_id AND cc.contact_value LIKE {pName}))";
            parameters.Add(pName, $"%{searchWords[i]}%");
        }

        return (whereClause, parameters);
    }

    public static string GetOrderBy(string sortBy, bool sortDescending)
    {
        var orderBy = sortBy.ToLower() switch
        {
            "firstname" => "c.first_name",
            "lastname" => "c.last_name",
            "companyname" => "c.company_name",
            "clienttype" => "ct.type_name",
            "city" => "City",
            "primarycontact" => "PrimaryContact",
            "activepoliciescount" => "ActivePoliciesCount",
            _ => "c.client_id"
        };
        
        return orderBy + (sortDescending ? " DESC" : " ASC");
    }

    /// <summary>
    /// Returns the main SQL query for fetching client data, including subqueries for primary contact, city, and active policies.
    /// </summary>
    public static string GetMainSql(ISqlDialect sqlDialect, string whereClause, string orderBy) => $@"
            SELECT 
                c.client_id AS ClientId, 
                c.first_name AS FirstName, 
                c.last_name AS LastName, 
                c.company_name AS CompanyName,
                ct.type_name AS ClientType,
                (SELECT {sqlDialect.Top(1)} cc.contact_value FROM client_contacts cc WHERE cc.client_id = c.client_id AND cc.is_primary = 1 {sqlDialect.Limit(1)}) AS PrimaryContact,
                (SELECT {sqlDialect.Top(1)} ca.city FROM client_addresses ca WHERE ca.client_id = c.client_id AND ca.is_current = 1 {sqlDialect.Limit(1)}) AS City,
                (SELECT COUNT(*) FROM policies p JOIN policy_statuses ps ON p.status_id = ps.status_id WHERE p.client_id = c.client_id AND ps.is_active_policy = 1) AS ActivePoliciesCount
            FROM clients c
            JOIN client_types ct ON c.client_type_id = ct.client_type_id
            {whereClause}
            ORDER BY {orderBy}
            {sqlDialect.Paging("@Offset", "@PageSize")}";
}
