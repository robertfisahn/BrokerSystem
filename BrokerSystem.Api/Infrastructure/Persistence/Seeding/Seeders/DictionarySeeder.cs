using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Seeding.Seeders;
/// <summary>
/// Seeder dla słowników (WARSTWA 0 - zero zależności)
/// </summary>
public class DictionarySeeder
{
    private readonly BrokerSystemDbContext _context;
    private readonly ILogger _logger;

    public DictionarySeeder(BrokerSystemDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedClientTypesAsync();
        await SeedPolicyCategoriesAsync();
        await SeedPolicyTypesAsync();
        await SeedPolicyStatusesAsync();
        await SeedClaimStatusesAsync();
        await SeedPaymentMethodsAsync();
        await SeedRiskLevelsAsync();
        await SeedRolesAsync();
        await SeedCommissionStatusesAsync();
        await SeedPaymentStatusesAsync();
    }

    private async Task SeedClientTypesAsync()
    {
        if (await _context.ClientTypes.AnyAsync()) return;

        var types = new List<ClientType>
        {
            new() { TypeName = "B2C", Description = "Klienci indywidualni", DiscountRate = 0 },
            new() { TypeName = "B2B", Description = "Małe i średnie firmy", DiscountRate = 5 },
            new() { TypeName = "VIP", Description = "Klienci premium (>5 polis lub >10k/rok)", DiscountRate = 15 },
            new() { TypeName = "Corporate", Description = "Duże korporacje (>50 pracowników)", DiscountRate = 20 }
        };

        await _context.ClientTypes.AddRangeAsync(types);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"  ✓ Client Types: {types.Count}");
    }

    private async Task SeedPolicyCategoriesAsync()
    {
        if (await _context.PolicyCategories.AnyAsync()) return;

        var categories = new List<PolicyCategory>();

        // Poziom 1: Root
        var insurance = new PolicyCategory { CategoryName = "Insurance", ParentCategoryId = null, Level = 1, Path = "1" };
        categories.Add(insurance);
        await _context.PolicyCategories.AddAsync(insurance);
        await _context.SaveChangesAsync(); // Save to get ID

        // Poziom 2: Główne kategorie
        var life = new PolicyCategory { CategoryName = "Life", ParentCategoryId = insurance.CategoryId, Level = 2, Path = $"1.{insurance.CategoryId}" };
        var property = new PolicyCategory { CategoryName = "Property", ParentCategoryId = insurance.CategoryId, Level = 2, Path = $"1.{insurance.CategoryId}" };
        var auto = new PolicyCategory { CategoryName = "Auto", ParentCategoryId = insurance.CategoryId, Level = 2, Path = $"1.{insurance.CategoryId}" };
        var health = new PolicyCategory { CategoryName = "Health", ParentCategoryId = insurance.CategoryId, Level = 2, Path = $"1.{insurance.CategoryId}" };

        categories.AddRange(new[] { life, property, auto, health });
        await _context.PolicyCategories.AddRangeAsync(new[] { life, property, auto, health });
        await _context.SaveChangesAsync();

        // Poziom 3: Podkategorie
        var termLife = new PolicyCategory { CategoryName = "Term Life", ParentCategoryId = life.CategoryId, Level = 3, Path = $"1.{insurance.CategoryId}.{life.CategoryId}" };
        var wholeLife = new PolicyCategory { CategoryName = "Whole Life", ParentCategoryId = life.CategoryId, Level = 3, Path = $"1.{insurance.CategoryId}.{life.CategoryId}" };

        var home = new PolicyCategory { CategoryName = "Home", ParentCategoryId = property.CategoryId, Level = 3, Path = $"1.{insurance.CategoryId}.{property.CategoryId}" };
        var apartment = new PolicyCategory { CategoryName = "Apartment", ParentCategoryId = property.CategoryId, Level = 3, Path = $"1.{insurance.CategoryId}.{property.CategoryId}" };

        var liability = new PolicyCategory { CategoryName = "Liability (OC)", ParentCategoryId = auto.CategoryId, Level = 3, Path = $"1.{insurance.CategoryId}.{auto.CategoryId}" };
        var comprehensive = new PolicyCategory { CategoryName = "Comprehensive (AC)", ParentCategoryId = auto.CategoryId, Level = 3, Path = $"1.{insurance.CategoryId}.{auto.CategoryId}" };

        categories.AddRange(new[] { termLife, wholeLife, home, apartment, liability, comprehensive });
        await _context.PolicyCategories.AddRangeAsync(new[] { termLife, wholeLife, home, apartment, liability, comprehensive });
        await _context.SaveChangesAsync();

        _logger.LogInformation($"  ✓ Policy Categories (hierarchia): {categories.Count}");
    }

    private async Task SeedPolicyTypesAsync()
    {
        if (await _context.PolicyTypes.AnyAsync()) return;

        var autoCategory = await _context.PolicyCategories.FirstAsync(c => c.CategoryName == "Auto");
        var lifeCategory = await _context.PolicyCategories.FirstAsync(c => c.CategoryName == "Life");
        var propertyCategory = await _context.PolicyCategories.FirstAsync(c => c.CategoryName == "Property");
        var healthCategory = await _context.PolicyCategories.FirstAsync(c => c.CategoryName == "Health");

        var types = new List<PolicyType>
        {
            // Auto
            new() { CategoryId = autoCategory.CategoryId, TypeName = "OC pojazdu", BasePremium = 800, IsActive = true },
            new() { CategoryId = autoCategory.CategoryId, TypeName = "AC pojazdu", BasePremium = 1500, IsActive = true },
            new() { CategoryId = autoCategory.CategoryId, TypeName = "NNW kierowcy", BasePremium = 200, IsActive = true },
            new() { CategoryId = autoCategory.CategoryId, TypeName = "Assistance", BasePremium = 300, IsActive = true },
            
            // Life
            new() { CategoryId = lifeCategory.CategoryId, TypeName = "Życiowe terminowe", BasePremium = 500, IsActive = true },
            new() { CategoryId = lifeCategory.CategoryId, TypeName = "Życiowe całe życie", BasePremium = 1200, IsActive = true },
            new() { CategoryId = lifeCategory.CategoryId, TypeName = "Życiowe z UFK", BasePremium = 2000, IsActive = true },
            
            // Property
            new() { CategoryId = propertyCategory.CategoryId, TypeName = "Mieszkania", BasePremium = 600, IsActive = true },
            new() { CategoryId = propertyCategory.CategoryId, TypeName = "Domu", BasePremium = 1000, IsActive = true },
            new() { CategoryId = propertyCategory.CategoryId, TypeName = "Mienia ruchomego", BasePremium = 400, IsActive = true },
            
            // Health
            new() { CategoryId = healthCategory.CategoryId, TypeName = "Zdrowotne", BasePremium = 300, IsActive = true },
            new() { CategoryId = healthCategory.CategoryId, TypeName = "Dentystyczne", BasePremium = 150, IsActive = true }
        };

        await _context.PolicyTypes.AddRangeAsync(types);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"  ✓ Policy Types: {types.Count}");
    }

    private async Task SeedPolicyStatusesAsync()
    {
        if (await _context.PolicyStatuses.AnyAsync()) return;

        var statuses = new List<PolicyStatus>
        {
            new() { StatusName = "draft", Description = "Projekt polisy", IsActivePolicy = false },
            new() { StatusName = "active", Description = "Polisa aktywna", IsActivePolicy = true },
            new() { StatusName = "suspended", Description = "Polisa zawieszona", IsActivePolicy = false },
            new() { StatusName = "cancelled", Description = "Polisa anulowana", IsActivePolicy = false },
            new() { StatusName = "expired", Description = "Polisa wygasła", IsActivePolicy = false },
            new() { StatusName = "renewed", Description = "Polisa odnowiona", IsActivePolicy = false }
        };

        await _context.PolicyStatuses.AddRangeAsync(statuses);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"  ✓ Policy Statuses: {statuses.Count}");
    }

    private async Task SeedClaimStatusesAsync()
    {
        if (await _context.ClaimStatuses.AnyAsync()) return;

        var statuses = new List<ClaimStatus>
        {
            new() { StatusName = "reported", IsFinal = false },
            new() { StatusName = "under_review", IsFinal = false },
            new() { StatusName = "approved", IsFinal = false },
            new() { StatusName = "rejected", IsFinal = true },
            new() { StatusName = "paid", IsFinal = true },
            new() { StatusName = "closed", IsFinal = true },
            new() { StatusName = "reopened", IsFinal = false }
        };

        await _context.ClaimStatuses.AddRangeAsync(statuses);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"  ✓ Claim Statuses: {statuses.Count}");
    }

    private async Task SeedPaymentMethodsAsync()
    {
        if (await _context.PaymentMethods.AnyAsync()) return;

        var methods = new List<PaymentMethod>
        {
            new() { MethodName = "bank_transfer" },
            new() { MethodName = "credit_card" },
            new() { MethodName = "debit_card" },
            new() { MethodName = "cash" },
            new() { MethodName = "PayPal" }
        };

        await _context.PaymentMethods.AddRangeAsync(methods);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"  ✓ Payment Methods: {methods.Count}");
    }

    private async Task SeedRiskLevelsAsync()
    {
        if (await _context.RiskLevels.AnyAsync()) return;

        var levels = new List<RiskLevel>
        {
            new() { LevelName = "very_low", PremiumMultiplier = 0.8m },
            new() { LevelName = "low", PremiumMultiplier = 1.0m },
            new() { LevelName = "medium", PremiumMultiplier = 1.3m },
            new() { LevelName = "high", PremiumMultiplier = 1.7m },
            new() { LevelName = "very_high", PremiumMultiplier = 2.5m }
        };

        await _context.RiskLevels.AddRangeAsync(levels);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"  ✓ Risk Levels: {levels.Count}");
    }

    private async Task SeedRolesAsync()
    {
        if (await _context.Roles.AnyAsync()) return;

        var roles = new List<Role>
        {
            new() { RoleName = "admin" },
            new() { RoleName = "agent" },
            new() { RoleName = "underwriter" },
            new() { RoleName = "manager" },
            new() { RoleName = "viewer" }
        };

        await _context.Roles.AddRangeAsync(roles);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"  ✓ Roles: {roles.Count}");
    }

    private async Task SeedCommissionStatusesAsync()
    {
        if (await _context.CommissionStatuses.AnyAsync()) return;

        var statuses = new List<CommissionStatus>
        {
            new() { StatusName = "paid" },
            new() { StatusName = "pending" },
            new() { StatusName = "rejected" }
        };

        await _context.CommissionStatuses.AddRangeAsync(statuses);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"  ✓ Commission Statuses: {statuses.Count}");
    }

    private async Task SeedPaymentStatusesAsync()
    {
        if (await _context.PaymentStatuses.AnyAsync()) return;

        var statuses = new List<PaymentStatus>
        {
            new() { StatusName = "completed" },
            new() { StatusName = "pending" },
            new() { StatusName = "failed" },
            new() { StatusName = "partially_paid" }
        };

        await _context.PaymentStatuses.AddRangeAsync(statuses);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"  ✓ Payment Statuses: {statuses.Count}");
    }
}