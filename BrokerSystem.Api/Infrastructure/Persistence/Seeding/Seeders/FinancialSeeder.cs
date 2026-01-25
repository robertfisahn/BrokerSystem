using Bogus;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using BrokerSystem.Api.Infrastructure.Persistence.Seeding.Configuration;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Seeding.Seeders;

/// <summary>
/// Seeder dla finansów (WARSTWA 5)
/// Tworzy płatności składek dla polis
/// </summary>
public class FinancialSeeder
{
    private readonly BrokerSystemDbContext _context;
    private readonly ILogger _logger;
    private readonly Faker _faker = new("pl");

    public FinancialSeeder(BrokerSystemDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _context.Payments.AnyAsync())
        {
            _logger.LogInformation("  ⚠ Płatności już istnieją, pomijam...");
            return;
        }

        var startTime = DateTime.Now;

        var policies = await _context.Policies
            .Include(p => p.Status)
            .ToListAsync();

        var paymentMethods = await _context.PaymentMethods.ToListAsync();
        var statuses = await _context.PaymentStatuses.ToListAsync();
        var completedStatus = statuses.First(s => s.StatusName == "completed");
        var pendingStatus = statuses.First(s => s.StatusName == "pending");
        var failedStatus = statuses.First(s => s.StatusName == "failed");

        _logger.LogInformation($"  Generowanie płatności dla {policies.Count} polis...");

        var payments = new List<Payment>();

        foreach (var policy in policies)
        {
            // Liczba płatności zależy od payment_frequency i okresu trwania polisy
            int totalPayments = CalculateExpectedPayments(policy);

            // Aktywne polisy: wszystkie płatności completed
            // Inne: 80-95% completed, reszta pending/failed
            float completionRate = policy.Status.IsActivePolicy ? 1.0f : _faker.Random.Float(0.8f, 0.95f);
            int completedPayments = (int)(totalPayments * completionRate);

            for (int i = 0; i < totalPayments; i++)
            {
                var paymentDate = CalculatePaymentDate(policy, i);

                // Skip jeśli data w przyszłości
                if (paymentDate > DateTime.Now) continue;

                var payment = new Payment
                {
                    PolicyId = policy.PolicyId,
                    Amount = CalculatePaymentAmount(policy),
                    PaymentDate = DateOnly.FromDateTime(paymentDate),
                    PaymentMethodId = _faker.PickRandom(paymentMethods).MethodId,
                    PaymentStatusId = i < completedPayments ? completedStatus.PaymentStatusId : _faker.Random.Bool(0.5f) ? pendingStatus.PaymentStatusId : failedStatus.PaymentStatusId,
                    TransactionId = BogusConfiguration.NextTransactionId()
                };

                payments.Add(payment);
            }
        }

        _logger.LogInformation($"    Wygenerowano {payments.Count} płatności");

        // Zapisz w batch'ach
        int saved = 0;
        foreach (var batch in payments.Chunk(1000))
        {
            await _context.Payments.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
            saved += batch.Length;
            _logger.LogInformation($"    Zapisano {saved}/{payments.Count} płatności...");
        }

        var duration = (DateTime.Now - startTime).TotalSeconds;
        _logger.LogInformation($"  ✓ Payments: {payments.Count} ({duration:F1}s)");
    }

    /// <summary>
    /// Oblicza oczekiwaną liczbę płatności dla polisy
    /// </summary>
    private int CalculateExpectedPayments(Policy policy)
    {
        var duration = (policy.EndDate.ToDateTime(TimeOnly.MinValue) - policy.StartDate.ToDateTime(TimeOnly.MinValue)).Days;
        var years = duration / 365.0;

        return policy.PaymentFrequency switch
        {
            "monthly" => (int)Math.Ceiling(years * 12),
            "quarterly" => (int)Math.Ceiling(years * 4),
            "semi-annual" => (int)Math.Ceiling(years * 2),
            "annual" => (int)Math.Ceiling(years),
            _ => 1
        };
    }

    /// <summary>
    /// Oblicza datę konkretnej płatności
    /// </summary>
    private DateTime CalculatePaymentDate(Policy policy, int paymentIndex)
    {
        var daysToAdd = policy.PaymentFrequency switch
        {
            "monthly" => 30 * paymentIndex,
            "quarterly" => 90 * paymentIndex,
            "semi-annual" => 180 * paymentIndex,
            "annual" => 365 * paymentIndex,
            _ => 0
        };

        return policy.StartDate.ToDateTime(TimeOnly.MinValue).AddDays(daysToAdd);
    }

    /// <summary>
    /// Oblicza kwotę pojedynczej płatności
    /// </summary>
    private decimal CalculatePaymentAmount(Policy policy)
    {
        return policy.PaymentFrequency switch
        {
            "monthly" => policy.PremiumAmount / 12,
            "quarterly" => policy.PremiumAmount / 4,
            "semi-annual" => policy.PremiumAmount / 2,
            "annual" => policy.PremiumAmount,
            _ => policy.PremiumAmount
        };
    }
}