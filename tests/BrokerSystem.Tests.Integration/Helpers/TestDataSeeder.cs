using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Tests.Integration.Helpers;

public static class TestDataSeeder
{
    public static async Task SeedAsync(BrokerSystemDbContext context)
    {
        await SeedDictionariesAsync(context);
        await SeedTestDataAsync(context);
    }

    private static async Task SeedDictionariesAsync(BrokerSystemDbContext context)
    {
        if (!await context.PolicyStatuses.AnyAsync())
        {
            context.PolicyStatuses.AddRange(
                new PolicyStatus { StatusName = "Active", IsActivePolicy = true },
                new PolicyStatus { StatusName = "Draft", IsActivePolicy = false }
            );
        }

        if (!await context.PolicyTypes.AnyAsync())
        {
            var category = new PolicyCategory { CategoryName = "Auto", Level = 1 };
            context.PolicyCategories.Add(category);
            await context.SaveChangesAsync();

            context.PolicyTypes.Add(new PolicyType 
            { 
                TypeName = "OC vehicle", 
                CategoryId = category.CategoryId, 
                BasePremium = 1000, 
                IsActive = true 
            });
        }

        if (!await context.ClientTypes.AnyAsync())
        {
            context.ClientTypes.AddRange(
                new ClientType { TypeName = "Individual" },
                new ClientType { TypeName = "Corporate" },
                new ClientType { TypeName = "VIP" }
            );
        }

        if (!await context.ClaimStatuses.AnyAsync())
        {
            context.ClaimStatuses.AddRange(
                new ClaimStatus { StatusName = "Submitted", IsFinal = false },
                new ClaimStatus { StatusName = "Approved", IsFinal = true }
            );
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedTestDataAsync(BrokerSystemDbContext context)
    {
        if (!await context.Clients.AnyAsync())
        {
            // Client 1: Individual, Registered Today (This Month)
            context.Clients.Add(new Client 
            { 
                FirstName = "Test", 
                LastName = "User", 
                TaxId = "1234567890",
                ClientTypeId = 1,
                RegistrationDate = DateOnly.FromDateTime(DateTime.Today),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // Client 2: Corporate, Registered Today (This Month)
            context.Clients.Add(new Client 
            { 
                FirstName = "Corp", 
                LastName = "Client", 
                TaxId = "CORP123",
                ClientTypeId = 2,
                RegistrationDate = DateOnly.FromDateTime(DateTime.Today),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // Client 3: VIP, Registered Today (This Month)
            context.Clients.Add(new Client 
            { 
                FirstName = "VIP", 
                LastName = "Client", 
                TaxId = "VIP999",
                ClientTypeId = 3,
                RegistrationDate = DateOnly.FromDateTime(DateTime.Today),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // Client 4: Individual, Registered Last Month (Not This Month)
            context.Clients.Add(new Client 
            { 
                FirstName = "Old", 
                LastName = "Client", 
                TaxId = "OLD123",
                ClientTypeId = 1,
                RegistrationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // Client 5: Individual, Krakow
            context.Clients.Add(new Client 
            { 
                FirstName = "Krakow", 
                LastName = "User", 
                TaxId = "KRK123",
                ClientTypeId = 1,
                RegistrationDate = DateOnly.FromDateTime(DateTime.Today),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!await context.Agents.AnyAsync())
        {
            context.Agents.Add(new Agent 
            { 
                FirstName = "Super", 
                LastName = "Agent",
                Email = "agent@example.com",
                Phone = "123-456-789",
                HireDate = DateOnly.FromDateTime(DateTime.Today),
                CommissionRate = 0.1m,
                IsActive = true
            });
        }
        
        if (!await context.Policies.AnyAsync())
        {
            var policy = new Policy 
            { 
                ClientId = 1,
                PolicyNumber = "POL/TEST/001",
                PolicyTypeId = 1,
                StatusId = 1,
                AgentId = 1,
                PremiumAmount = 1500.00m,
                SumInsured = 50000.00m,
                PaymentFrequency = "Monthly",
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Policies.Add(policy);
            await context.SaveChangesAsync(); // Save to get the PolicyId for claims

            context.Claims.Add(new Claim
            {
                PolicyId = policy.PolicyId,
                ClaimNumber = "CLM/001",
                StatusId = 1,
                IncidentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                ReportedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                Description = "Minor collision",
                ClaimedAmount = 500,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await context.ClientAddresses.AnyAsync())
        {
            context.ClientAddresses.Add(new ClientAddress
            {
                ClientId = 1,
                AddressType = "Mailing",
                City = "Warsaw",
                PostalCode = "00-001",
                Street = "Test Street 10",
                Country = "Poland",
                IsCurrent = true,
                ValidFrom = DateOnly.FromDateTime(DateTime.Today)
            });

            context.ClientAddresses.Add(new ClientAddress
            {
                ClientId = 5,
                AddressType = "Mailing",
                City = "Krakow",
                PostalCode = "31-001",
                Street = "Florianska 1",
                Country = "Poland",
                IsCurrent = true,
                ValidFrom = DateOnly.FromDateTime(DateTime.Today)
            });
        }

        if (!await context.ClientContacts.AnyAsync())
        {
            context.ClientContacts.Add(new ClientContact
            {
                ClientId = 1,
                ContactType = "Email",
                ContactValue = "test@user.com",
                IsPrimary = true
            });
        }

        await context.SaveChangesAsync();
    }
}
