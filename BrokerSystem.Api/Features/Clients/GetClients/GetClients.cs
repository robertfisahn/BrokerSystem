using BrokerSystem.Api.Common.Models;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Dapper;

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
        using var connection = db.Database.GetDbConnection();

        var whereClause = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchWords = request.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < searchWords.Length; i++)
            {
                var pName = $"@p{i}";
                whereClause += $" AND (c.first_name LIKE {pName} OR c.last_name LIKE {pName} OR c.company_name LIKE {pName})";
                parameters.Add(pName, $"%{searchWords[i]}%");
            }
        }

        // 2. Count Query
        var countSql = $"SELECT COUNT(*) FROM clients c {whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // 3. Sorting
        var orderBy = request.SortBy.ToLower() switch
        {
            "firstname" => "c.first_name",
            "lastname" => "c.last_name",
            "companyname" => "c.company_name",
            "clienttype" => "ct.type_name",
            _ => "c.client_id"
        };
        orderBy += request.SortDescending ? " DESC" : " ASC";

        // 4. Data Query (Dapper Optimized)
        var sql = @$"
            SELECT 
                c.client_id AS ClientId, 
                c.first_name AS FirstName, 
                c.last_name AS LastName, 
                c.company_name AS CompanyName,
                ct.type_name AS ClientType,
                (SELECT TOP 1 cc.contact_value FROM client_contacts cc WHERE cc.client_id = c.client_id AND cc.is_primary = 1) AS PrimaryContact,
                (SELECT TOP 1 ca.city FROM client_addresses ca WHERE ca.client_id = c.client_id AND ca.is_current = 1) AS City,
                (SELECT COUNT(*) FROM policies p JOIN policy_statuses ps ON p.status_id = ps.status_id WHERE p.client_id = c.client_id AND ps.is_active_policy = 1) AS ActivePoliciesCount
            FROM clients c
            JOIN client_types ct ON c.client_type_id = ct.client_type_id
            {whereClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        parameters.Add("@Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("@PageSize", request.PageSize);

        var items = (await connection.QueryAsync<GetClientsDto>(sql, parameters)).ToList();

        return new PaginatedResult<GetClientsDto>(items, totalCount, request.Page, request.PageSize);
    }
}
