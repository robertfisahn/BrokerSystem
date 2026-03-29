using BrokerSystem.Api.Common.Auth;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Common.Endpoints;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Dapper;
using BrokerSystem.Api.Infrastructure.Persistence;

namespace BrokerSystem.Api.Features.Dashboard;

public class GetAgentDashboardEndpoint : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("api/dashboard/agent", async (IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetAgentDashboardQuery())))
            .WithName("GetAgentDashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();
    }
}

public record GetAgentDashboardQuery : IRequest<AgentDashboardResponse>;

public record AgentDashboardResponse(
    AgentStatsDto Stats,
    List<ExpiringPolicyDto> ExpiringPolicies,
    List<RecentActivityDto> RecentActivities);

public record AgentStatsDto(
    int TotalClients,
    int ActivePolicies,
    decimal TotalPremium);

public record ExpiringPolicyDto(
    int PolicyId,
    string PolicyNumber,
    string ClientName,
    DateOnly EndDate,
    int DaysLeft);

public record RecentActivityDto(
    string Type,
    string Description,
    DateTime CreatedAt);

public class GetAgentDashboardHandler(
    BrokerSystemDbContext db,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetAgentDashboardQuery, AgentDashboardResponse>
{
    public async Task<AgentDashboardResponse> Handle(GetAgentDashboardQuery request, CancellationToken ct)
    {
        using var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        var sqlDialect = db.Database.Sql();
        var agentId = currentUserService.AgentId ?? 0;
        var userId = currentUserService.UserId;

        var today = DateTime.Today;
        var in30Days = today.AddDays(30);

        // Fetch everything in ONE round-trip
        var sql = $@"
            /* 1. Stats */
            SELECT 
                (SELECT COUNT(DISTINCT client_id) FROM policies WHERE agent_id = @AgentId) AS TotalClients,
                (SELECT COUNT(*) FROM policies p JOIN policy_statuses ps ON p.status_id = ps.status_id WHERE p.agent_id = @AgentId AND ps.status_name = 'active') AS ActivePolicies,
                {sqlDialect.IsNull("(SELECT SUM(premium_amount) FROM policies p JOIN policy_statuses ps ON p.status_id = ps.status_id WHERE p.agent_id = @AgentId AND ps.status_name = 'active')", "0")} AS TotalPremium;

            /* 2. Expiring Policies (top 5) */
            SELECT {sqlDialect.Top(5)}
                p.policy_id AS PolicyId, 
                p.policy_number AS PolicyNumber, 
                {sqlDialect.Concat("c.first_name", "' '", "c.last_name")} AS ClientName, 
                p.end_date AS EndDate,
                {sqlDialect.DateDiffDay("@Today", "p.end_date")} AS DaysLeft
            FROM policies p
            JOIN clients c ON p.client_id = c.client_id
            JOIN policy_statuses ps ON p.status_id = ps.status_id
            WHERE p.agent_id = @AgentId 
              AND ps.status_name = 'active' 
              AND p.end_date >= CAST(@Today AS DATE) 
              AND p.end_date <= CAST(@In30Days AS DATE)
            ORDER BY p.end_date
            {sqlDialect.Limit(5)};

            /* 3. Recent Activities (top 10) */
            SELECT {sqlDialect.Top(10)}
                activity_type AS Type, 
                description AS Description, 
                created_at AS CreatedAt
            FROM user_activities
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            {sqlDialect.Limit(10)};
        ";

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            AgentId = agentId,
            UserId = userId,
            Today = today,
            In30Days = in30Days
        });

        var stats = await multi.ReadSingleAsync<AgentStatsDto>();
        var expiringPolicies = (await multi.ReadAsync<ExpiringPolicyDto>()).ToList();
        var recentActivities = (await multi.ReadAsync<RecentActivityDto>()).ToList();

        return new AgentDashboardResponse(stats, expiringPolicies, recentActivities);
    }
}
