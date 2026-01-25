using Bogus;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using BrokerSystem.Api.Infrastructure.Persistence.Seeding.Configuration;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Seeding.Seeders;

/// <summary>
/// Seeder dla roszczeń/szkód (WARSTWA 4)
/// Tworzy szkody tylko dla aktywnych polis z logicznymi datami
/// </summary>
public class ClaimSeeder
{
    private readonly BrokerSystemDbContext _context;
    private readonly ILogger _logger;
    private readonly Faker _faker = new("pl");

    public ClaimSeeder(BrokerSystemDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(int totalClaims = 800)
    {
        if (await _context.Claims.AnyAsync())
        {
            _logger.LogInformation("  ⚠ Roszczenia już istnieją, pomijam...");
            return;
        }

        var startTime = DateTime.Now;

        // Pobierz polisy które mogą mieć szkody (aktywne lub expired, które trwały >6 miesięcy)
        var eligiblePolicies = await _context.Policies
            .Include(p => p.PolicyType)
            .Where(p => p.StartDate < DateOnly.FromDateTime(DateTime.Now.AddMonths(-6))) // Polisa musi trwać >6 msc
            .ToListAsync();

        _logger.LogInformation($"  Generowanie {totalClaims} szkód...");
        _logger.LogInformation($"    Polis kwalifikujących się: {eligiblePolicies.Count}");

        var statuses = await _context.ClaimStatuses.ToListAsync();
        var reportedStatus = statuses.First(s => s.StatusName == "reported");
        var approvedStatus = statuses.First(s => s.StatusName == "approved");
        var paidStatus = statuses.First(s => s.StatusName == "paid");
        var rejectedStatus = statuses.First(s => s.StatusName == "rejected");

        var claims = new List<Claim>();

        for (int i = 0; i < totalClaims; i++)
        {
            var policy = _faker.PickRandom(eligiblePolicies);

            // Status z wagami: 50% paid, 25% approved, 15% reported, 10% rejected
            var status = _faker.Random.WeightedRandom(
                new[] { paidStatus, approvedStatus, reportedStatus, rejectedStatus },
                new[] { 0.5f, 0.25f, 0.15f, 0.1f }
            );

            var claim = BogusConfiguration.GetClaimFaker(
                policy.PolicyId,
                policy,
                status.StatusId
            ).Generate();

            // Dla rejected i reported claims, brak approved_amount
            if (status.StatusName == "rejected" || status.StatusName == "reported")
            {
                claim.ApprovedAmount = null;
            }
            // Dla paid i approved MUSI być kwota
            else if (claim.ApprovedAmount == null)
            {
                claim.ApprovedAmount = Math.Round(claim.ClaimedAmount * _faker.Random.Decimal(0.8m, 1.0m), 2);
            }

            claims.Add(claim);
        }

        // Zapisz szkody
        foreach (var batch in claims.Chunk(500))
        {
            await _context.Claims.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
        }

        // Seeduj powiązane dane
        await SeedClaimStatusHistoryAsync(claims, statuses);
        await SeedClaimPaymentsAsync(claims);

        var duration = (DateTime.Now - startTime).TotalSeconds;
        _logger.LogInformation($"  ✓ Claims: {totalClaims} ({duration:F1}s)");
    }

    private async Task SeedClaimStatusHistoryAsync(List<Claim> claims, List<ClaimStatus> statuses)
    {
        var history = new List<ClaimStatusHistory>();

        foreach (var claim in claims)
        {
            // Każda szkoda ma historię: reported → under_review → [approved/rejected] → [paid]
            var reportedStatus = statuses.First(s => s.StatusName == "reported");
            var underReviewStatus = statuses.First(s => s.StatusName == "under_review");

            // 1. Zgłoszenie
            history.Add(new ClaimStatusHistory
            {
                ClaimId = claim.ClaimId,
                OldStatusId = null,
                NewStatusId = reportedStatus.StatusId,
                ChangedAt = claim.ReportedDate.ToDateTime(TimeOnly.MinValue),
                ChangedByUserId = _faker.Random.Number(1, 50),
                Notes = "Szkoda zgłoszona przez klienta"
            });

            // 2. W trakcie weryfikacji (1-3 dni później)
            var reviewDate = claim.ReportedDate.AddDays(_faker.Random.Number(1, 3));
            history.Add(new ClaimStatusHistory
            {
                ClaimId = claim.ClaimId,
                OldStatusId = reportedStatus.StatusId,
                NewStatusId = underReviewStatus.StatusId,
                ChangedAt = reviewDate.ToDateTime(TimeOnly.MinValue),
                ChangedByUserId = _faker.Random.Number(1, 50),
                Notes = "Rozpoczęto weryfikację szkody"
            });

            // 3. Zatwierdzenie lub odrzucenie (5-15 dni później)
            var finalDate = reviewDate.AddDays(_faker.Random.Number(5, 15));
            history.Add(new ClaimStatusHistory
            {
                ClaimId = claim.ClaimId,
                OldStatusId = underReviewStatus.StatusId,
                NewStatusId = claim.StatusId,
                ChangedAt = finalDate.ToDateTime(TimeOnly.MinValue),
                ChangedByUserId = _faker.Random.Number(1, 50),
                Notes = claim.Status.StatusName == "rejected"
                    ? "Szkoda odrzucona - brak podstaw do wypłaty"
                    : "Szkoda zatwierdzona"
            });

            // 4. Wypłata (jeśli paid) (3-7 dni po zatwierdzeniu)
            if (claim.Status.StatusName == "paid")
            {
                var paidDate = finalDate.AddDays(_faker.Random.Number(3, 7));
                var paidStatus = statuses.First(s => s.StatusName == "paid");

                history.Add(new ClaimStatusHistory
                {
                    ClaimId = claim.ClaimId,
                    OldStatusId = claim.StatusId,
                    NewStatusId = paidStatus.StatusId,
                    ChangedAt = paidDate.ToDateTime(TimeOnly.MinValue),
                    ChangedByUserId = _faker.Random.Number(1, 50),
                    Notes = "Odszkodowanie wypłacone"
                });
            }
        }

        foreach (var batch in history.Chunk(1000))
        {
            await _context.ClaimStatusHistories.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation($"    ✓ Claim Status History: {history.Count}");
    }

    private async Task SeedClaimPaymentsAsync(List<Claim> claims)
    {
        var payments = new List<ClaimPayment>();
        var paymentMethod = await _context.PaymentMethods.FirstAsync(m => m.MethodName == "bank_transfer");

        // Tylko paid claims mają płatności
        var paidClaims = claims.Where(c => c.Status.StatusName == "paid").ToList();

        foreach (var claim in paidClaims)
        {
            // 70% jednorázowo, 30% w ratach
            if (_faker.Random.Bool(0.7f))
            {
                // Jednorazowa płatność
                payments.Add(new ClaimPayment
                {
                    ClaimId = claim.ClaimId,
                    Amount = claim.ApprovedAmount!.Value,
                    PaymentDate = DateOnly.FromDateTime(claim.ReportedDate.ToDateTime(TimeOnly.MinValue).AddDays(_faker.Random.Number(10, 30))),
                    PaymentMethodId = paymentMethod.MethodId,
                    ReferenceNumber = BogusConfiguration.NextTransactionId()
                });
            }
            else
            {
                // Płatność w 2-3 ratach
                int installments = _faker.Random.Number(2, 3);
                decimal amountPerInstallment = claim.ApprovedAmount!.Value / installments;

                for (int i = 0; i < installments; i++)
                {
                    payments.Add(new ClaimPayment
                    {
                        ClaimId = claim.ClaimId,
                        Amount = Math.Round(amountPerInstallment, 2),
                        PaymentDate = DateOnly.FromDateTime(claim.ReportedDate.ToDateTime(TimeOnly.MinValue).AddDays(_faker.Random.Number(10 + (i * 15), 20 + (i * 15)))),
                        PaymentMethodId = paymentMethod.MethodId,
                        ReferenceNumber = $"{BogusConfiguration.NextTransactionId()}/{i + 1}"
                    });
                }
            }
        }

        await _context.ClaimPayments.AddRangeAsync(payments);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"    ✓ Claim Payments: {payments.Count}");
    }
}