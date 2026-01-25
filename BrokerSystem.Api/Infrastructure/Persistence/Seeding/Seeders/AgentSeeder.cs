using Bogus;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using BrokerSystem.Api.Infrastructure.Persistence.Seeding.Configuration;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace BrokerSystem.Api.Infrastructure.Persistence.Seeding.Seeders;

/// <summary>
/// Seeder dla agentów (WARSTWA 2)
/// Tworzy hierarchię agentów: CEO → Regional Managers → Team Leads → Agents
/// WAŻNE: Hierarchia jest kluczowa dla Recursive CTE!
/// </summary>
public class AgentSeeder
{
    private readonly BrokerSystemDbContext _context;
    private readonly ILogger _logger;

    public AgentSeeder(BrokerSystemDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(int totalAgents = 100)
    {
        if (await _context.Agents.AnyAsync())
        {
            _logger.LogInformation("  ⚠ Agenci już istnieją, pomijam...");
            return;
        }

        var startTime = DateTime.Now;
        var allAgents = new List<Agent>();

        // POZIOM 1: CEO (1 osoba)
        var ceo = BogusConfiguration.GetAgentFaker(null).Generate();
        ceo.FirstName = "Jan";
        ceo.LastName = "Kowalski";
        ceo.Email = "jan.kowalski@brokersystem.pl";
        ceo.CommissionRate = 5; // CEO ma niższą prowizję
        allAgents.Add(ceo);

        await _context.Agents.AddAsync(ceo);
        await _context.SaveChangesAsync(); // Save to get ID
        _logger.LogInformation("    ✓ CEO");

        // POZIOM 2: Regional Managers (5 osób)
        var regionalManagers = new List<Agent>();
        var regions = new[] { "Warsaw", "Krakow", "Wroclaw", "Poznan", "Gdansk" };

        foreach (var region in regions)
        {
            var rm = BogusConfiguration.GetAgentFaker(ceo.AgentId).Generate();
            rm.CommissionRate = 8;
            regionalManagers.Add(rm);
        }

        await _context.Agents.AddRangeAsync(regionalManagers);
        await _context.SaveChangesAsync();
        allAgents.AddRange(regionalManagers);
        _logger.LogInformation($"    ✓ Regional Managers: {regionalManagers.Count}");

        // POZIOM 3: Team Leads (każdy RM ma 3-4 TL = ~18 osób)
        var teamLeads = new List<Agent>();
        foreach (var rm in regionalManagers)
        {
            int teamLeadCount = new Faker().Random.Number(3, 4);

            for (int i = 0; i < teamLeadCount; i++)
            {
                var tl = BogusConfiguration.GetAgentFaker(rm.AgentId).Generate();
                tl.CommissionRate = 10;
                teamLeads.Add(tl);
            }
        }

        await _context.Agents.AddRangeAsync(teamLeads);
        await _context.SaveChangesAsync();
        allAgents.AddRange(teamLeads);
        _logger.LogInformation($"    ✓ Team Leads: {teamLeads.Count}");

        // POZIOM 4: Agents (każdy TL ma 4-5 agentów = ~76 osób)
        var agents = new List<Agent>();
        int remainingAgents = totalAgents - allAgents.Count;
        int agentsPerTL = remainingAgents / teamLeads.Count;

        foreach (var tl in teamLeads)
        {
            int agentCount = agentsPerTL;

            for (int i = 0; i < agentCount; i++)
            {
                var agent = BogusConfiguration.GetAgentFaker(tl.AgentId).Generate();
                agent.CommissionRate = new Faker().Random.Decimal(12, 18); // Agenci mają najwyższe prowizje
                agents.Add(agent);
            }
        }

        await _context.Agents.AddRangeAsync(agents);
        await _context.SaveChangesAsync();
        allAgents.AddRange(agents);
        _logger.LogInformation($"    ✓ Agents: {agents.Count}");

        // Seeduj performance dla ostatnich 24 miesięcy
        await SeedAgentPerformanceAsync(allAgents);

        // Utwórz użytkowników dla agentów
        await SeedUsersForAgentsAsync(allAgents);

        var duration = (DateTime.Now - startTime).TotalSeconds;
        _logger.LogInformation($"  ✓ Agenci (hierarchia): {allAgents.Count} ({duration:F1}s)");
        _logger.LogInformation($"    Struktura: 1 CEO → {regionalManagers.Count} RM → {teamLeads.Count} TL → {agents.Count} Agents");
    }

    private async Task SeedAgentPerformanceAsync(List<Agent> agents)
    {
        var performances = new List<AgentPerformance>();
        var faker = new Faker();

        foreach (var agent in agents)
        {
            // Ostatnie 24 miesiące
            for (int monthsAgo = 0; monthsAgo < 24; monthsAgo++)
            {
                var date = DateTime.Now.AddMonths(-monthsAgo);

                // Realistyczne dane: managerowie mają mniej sprzedaży, agenci więcej
                int policiesSold = agent.ManagerId == null ? 0 : // CEO nie sprzedaje
                                   agents.Any(a => a.ManagerId == agent.AgentId) ? faker.Random.Number(1, 5) : // Managerowie
                                   faker.Random.Number(3, 15); // Agenci

                var performance = new AgentPerformance
                {
                    AgentId = agent.AgentId,
                    Year = date.Year,
                    Month = date.Month,
                    PoliciesSold = policiesSold,
                    TotalPremium = policiesSold * faker.Random.Decimal(800, 3000),
                    TotalCommission = 0, // Zostanie przeliczone po seedowaniu polis
                    CustomerSatisfactionScore = faker.Random.Decimal(3.5m, 5.0m)
                };

                performance.TotalCommission = performance.TotalPremium * (agent.CommissionRate / 100);

                performances.Add(performance);
            }
        }

        foreach (var batch in performances.Chunk(1000))
        {
            await _context.AgentPerformances.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation($"    ✓ Agent Performance: {performances.Count}");
    }

    private async Task SeedUsersForAgentsAsync(List<Agent> agents)
    {
        var users = new List<User>();
        var adminRole = await _context.Roles.FirstAsync(r => r.RoleName == "admin");
        var managerRole = await _context.Roles.FirstAsync(r => r.RoleName == "manager");
        var agentRole = await _context.Roles.FirstAsync(r => r.RoleName == "agent");

        foreach (var agent in agents)
        {
            var user = new User
            {
                Username = agent.Email.Split('@')[0],
                Email = agent.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"), // Demo password
                AgentId = agent.AgentId,
                IsActive = agent.IsActive,
                CreatedAt = agent.HireDate.ToDateTime(TimeOnly.MinValue)
            };

            users.Add(user);
        }

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Przypisz role
        var userRoles = new List<UserRole>();

        foreach (var user in users)
        {
            var agent = agents.First(a => a.AgentId == user.AgentId);

            // CEO = admin
            if (agent.ManagerId == null)
            {
                userRoles.Add(new UserRole { UserId = user.UserId, RoleId = adminRole.RoleId });
            }
            // Regional Managers i Team Leads = manager + agent
            else if (agents.Any(a => a.ManagerId == agent.AgentId))
            {
                userRoles.Add(new UserRole { UserId = user.UserId, RoleId = managerRole.RoleId });
                userRoles.Add(new UserRole { UserId = user.UserId, RoleId = agentRole.RoleId });
            }
            // Zwykli agenci = agent
            else
            {
                userRoles.Add(new UserRole { UserId = user.UserId, RoleId = agentRole.RoleId });
            }
        }

        await _context.UserRoles.AddRangeAsync(userRoles);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"    ✓ Users: {users.Count}");
        _logger.LogInformation($"    ✓ User Roles: {userRoles.Count}");
    }
}