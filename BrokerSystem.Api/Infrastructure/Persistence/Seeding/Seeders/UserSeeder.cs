using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Seeding.Seeders;

/// <summary>
/// Seeder for fixed demo users (Admin and Agent) to facilitate easy login for manual testing.
/// </summary>
public class UserSeeder(BrokerSystemDbContext context, ILogger logger)
{
    public async Task SeedAsync()
    {
        var agentRole = await context.Roles.FirstAsync(r => r.RoleName == "Agent");
        var adminRole = await context.Roles.FirstAsync(r => r.RoleName == "Admin");

        // 1. Ensure Selling Agent exists for assignment
        var sellingAgent = await context.Agents
            .Where(a => a.ManagerId != null && !context.Agents.Any(x => x.ManagerId == a.AgentId))
            .OrderBy(a => a.AgentId)
            .FirstOrDefaultAsync() ?? await context.Agents.FirstAsync();

        // 2. Setup/Update Agent User
        var agentUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "agent1");
        if (agentUser == null)
        {
            agentUser = new User { Username = "agent1", CreatedAt = DateTime.UtcNow };
            await context.Users.AddAsync(agentUser);
        }
        
        agentUser.Email = "agent1@brokersystem.pl";
        agentUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("agent123");
        agentUser.AgentId = sellingAgent.AgentId;
        agentUser.IsActive = true;

        // 3. Setup/Update Admin User
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser == null)
        {
            adminUser = new User { Username = "admin", CreatedAt = DateTime.UtcNow };
            await context.Users.AddAsync(adminUser);
        }

        adminUser.Email = "admin@brokersystem.pl";
        adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        adminUser.AgentId = null;
        adminUser.IsActive = true;

        await context.SaveChangesAsync();

        // 4. Ensure Roles are assigned correctly (Clear others to avoid conflicts)
        var existingAgentRoles = await context.UserRoles.Where(ur => ur.UserId == agentUser.UserId).ToListAsync();
        context.UserRoles.RemoveRange(existingAgentRoles);
        await context.UserRoles.AddAsync(new UserRole { UserId = agentUser.UserId, RoleId = agentRole.RoleId });

        var existingAdminRoles = await context.UserRoles.Where(ur => ur.UserId == adminUser.UserId).ToListAsync();
        context.UserRoles.RemoveRange(existingAdminRoles);
        await context.UserRoles.AddAsync(new UserRole { UserId = adminUser.UserId, RoleId = adminRole.RoleId });

        await context.SaveChangesAsync();
        logger.LogInformation("    ✓ Static Demo Users Synchronized: agent1 (Agent), admin (Admin)");
    }
}
