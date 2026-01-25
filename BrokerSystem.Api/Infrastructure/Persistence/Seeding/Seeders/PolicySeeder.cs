using Bogus;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using BrokerSystem.Api.Infrastructure.Persistence.Seeding.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrokerSystem.Api.Infrastructure.Persistence.Seeding.Seeders;

/// <summary>
/// Seeder dla polis (WARSTWA 3 - GŁÓWNA TABELA)
/// Tworzy polisy z REALISTYCZNYMI powiązaniami i regułami biznesowymi
/// </summary>
public class PolicySeeder
{
    private readonly BrokerSystemDbContext _context;
    private readonly ILogger _logger;
    private readonly Faker _faker = new("pl");

    public PolicySeeder(BrokerSystemDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(int totalPolicies = 5000)
    {
        if (await _context.Policies.AnyAsync())
        {
            _logger.LogInformation("  ⚠ Polisy już istnieją, pomijam...");
            return;
        }

        var startTime = DateTime.Now;

        // Pobierz dane referencyjne
        var clients = await _context.Clients.Where(c => c.IsActive).ToListAsync();
        var policyTypes = await _context.PolicyTypes.Where(pt => pt.IsActive).ToListAsync();
        var agents = await _context.Agents
            .Where(a => a.IsActive && a.ManagerId != null) // Tylko agenci którzy sprzedają
            .ToListAsync();
        var activeStatus = await _context.PolicyStatuses.FirstAsync(s => s.StatusName == "active");
        var expiredStatus = await _context.PolicyStatuses.FirstAsync(s => s.StatusName == "expired");
        var cancelledStatus = await _context.PolicyStatuses.FirstAsync(s => s.StatusName == "cancelled");

        _logger.LogInformation($"  Generowanie {totalPolicies} polis...");
        _logger.LogInformation($"    Dostępnych klientów: {clients.Count}");
        _logger.LogInformation($"    Dostępnych agentów: {agents.Count}");
        _logger.LogInformation($"    Typów polis: {policyTypes.Count}");

        var policies = new List<Policy>();

        for (int i = 0; i < totalPolicies; i++)
        {
            // Realistyczny rozkład: VIP i Corporate mają więcej polis
            var client = PickWeightedClient(clients);
            var policyType = _faker.PickRandom(policyTypes);
            var agent = PickWeightedAgent(agents, client);

            // Status: 70% active, 20% expired, 10% cancelled
            var status = _faker.Random.WeightedRandom<PolicyStatus>(
                new[] { activeStatus, expiredStatus, cancelledStatus },
                new[] { 0.7f, 0.2f, 0.1f }
            );

            var policy = BogusConfiguration.GetPolicyFaker(
                client.ClientId,
                policyType.PolicyTypeId,
                agent.AgentId,
                status.StatusId,
                policyType.BasePremium
            ).Generate();

            // Zastosuj zniżki dla VIP/Corporate
            if (client.ClientType != null)
            {
                var discount = 1 - (client.ClientType.DiscountRate / 100);
                policy.PremiumAmount = Math.Round(policy.PremiumAmount * discount, 2);
            }

            // Dla expired polis, ustaw daty w przeszłości
            if (status.StatusName == "expired")
            {
                policy.StartDate = DateOnly.FromDateTime(_faker.Date.Between(DateTime.Now.AddYears(-5), DateTime.Now.AddYears(-1)));
                policy.EndDate = policy.StartDate.AddYears(1);
            }

            policies.Add(policy);
        }

        // Zapisz polisy (batch po 500)
        int saved = 0;
        foreach (var batch in policies.Chunk(500))
        {
            await _context.Policies.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
            saved += batch.Length;
            _logger.LogInformation($"    Zapisano {saved}/{totalPolicies} polis...");
        }

        // Seeduj powiązane dane
        await SeedPolicyStatusHistoryAsync(policies);
        await SeedPolicyBeneficiariesAsync(policies);
        await SeedInvoicesAsync(policies);
        await SeedCommissionsAsync(policies, agents);
        await SeedRiskAssessmentsAsync(policies);

        var duration = (DateTime.Now - startTime).TotalSeconds;
        _logger.LogInformation($"  ✓ Polisy: {totalPolicies} ({duration:F1}s)");
    }

    /// <summary>
    /// Wybiera klienta z wagami: VIP/Corporate mają większe szanse
    /// </summary>
    private Client PickWeightedClient(List<Client> clients)
    {
        var clientsByType = clients.GroupBy(c => c.ClientTypeId).ToList();

        // Wagi: VIP i Corporate mają więcej polis
        var weights = clientsByType.Select(g =>
        {
            var typeName = g.First().ClientType?.TypeName ?? "B2C";
            return typeName switch
            {
                "VIP" => 3.0f,
                "Corporate" => 4.0f,
                "B2B" => 1.5f,
                _ => 1.0f
            };
        }).ToArray();

        var selectedGroup = _faker.Random.WeightedRandom<IGrouping<int, Client>>(clientsByType.ToArray(), weights);
        return _faker.PickRandom(selectedGroup.ToList());
    }

    /// <summary>
    /// Wybiera agenta - preferuje agentów bez hierarchii (najwięcej sprzedają)
    /// </summary>
    private Agent PickWeightedAgent(List<Agent> agents, Client client)
    {
        // Agenci bez podwładnych (leaf nodes) mają większe szanse
        var leafAgents = agents.Where(a => !agents.Any(x => x.ManagerId == a.AgentId)).ToList();
        var managers = agents.Where(a => agents.Any(x => x.ManagerId == a.AgentId)).ToList();

        // 80% szans na leaf agenta, 20% na managera
        var selectedPool = _faker.Random.Bool(0.8f) ? leafAgents : managers;
        return _faker.PickRandom(selectedPool);
    }

    private async Task SeedPolicyStatusHistoryAsync(List<Policy> policies)
    {
        var history = new List<PolicyStatusHistory>();
        var statuses = await _context.PolicyStatuses.ToListAsync();

        foreach (var policy in policies)
        {
            // Każda polisa ma przynajmniej 1 wpis (utworzenie)
            history.Add(new PolicyStatusHistory
            {
                PolicyId = policy.PolicyId,
                OldStatusId = null,
                NewStatusId = policy.StatusId,
                ChangedAt = policy.StartDate.ToDateTime(TimeOnly.MinValue),
                ChangedByUserId = _faker.Random.Number(1, 50), // Losowy user
                Reason = "Polisa utworzona"
            });

            // 30% polis ma dodatkowe zmiany statusu
            if (_faker.Random.Bool(0.3f))
            {
                var changeDate = _faker.Date.Between(policy.StartDate.ToDateTime(TimeOnly.MinValue), DateTime.Now);
                var newStatus = _faker.PickRandom(statuses);

                history.Add(new PolicyStatusHistory
                {
                    PolicyId = policy.PolicyId,
                    OldStatusId = policy.StatusId,
                    NewStatusId = newStatus.StatusId,
                    ChangedAt = changeDate,
                    ChangedByUserId = _faker.Random.Number(1, 50),
                    Reason = _faker.PickRandom("Zmiana warunków", "Wniosek klienta", "Aktualizacja danych")
                });
            }
        }

        foreach (var batch in history.Chunk(1000))
        {
            await _context.PolicyStatusHistories.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation($"    ✓ Policy Status History: {history.Count}");
    }

    private async Task SeedPolicyBeneficiariesAsync(List<Policy> policies)
    {
        var beneficiaries = new List<PolicyBeneficiary>();

        // Tylko polisy życiowe mają beneficjentów
        var lifePolicies = policies
            .Where(p => p.PolicyType.TypeName.Contains("życiowe") || p.PolicyType.TypeName.Contains("Life"))
            .ToList();

        foreach (var policy in lifePolicies)
        {
            // 60% polis życiowych ma beneficjentów
            if (_faker.Random.Bool(0.6f))
            {
                int beneficiaryCount = _faker.Random.Number(1, 3);
                decimal remainingShare = 100m;

                for (int i = 0; i < beneficiaryCount; i++)
                {
                    decimal share = i == beneficiaryCount - 1
                        ? remainingShare
                        : _faker.Random.Decimal(20, remainingShare - 10);

                    beneficiaries.Add(new PolicyBeneficiary
                    {
                        PolicyId = policy.PolicyId,
                        FirstName = _faker.Name.FirstName(),
                        LastName = _faker.Name.LastName(),
                        Relationship = _faker.PickRandom("spouse", "child", "parent", "sibling"),
                        SharePercentage = Math.Round(share, 2)
                    });

                    remainingShare -= share;
                }
            }
        }

        await _context.PolicyBeneficiaries.AddRangeAsync(beneficiaries);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"    ✓ Policy Beneficiaries: {beneficiaries.Count}");
    }

    private async Task SeedInvoicesAsync(List<Policy> policies)
    {
        var invoices = new List<Invoice>();

        foreach (var policy in policies)
        {
            var totalNet = policy.PremiumAmount;
            var vatAmount = totalNet * 0.23m; // 23% VAT

            invoices.Add(new Invoice
            {
                InvoiceNumber = BogusConfiguration.NextInvoiceNumber(),
                PolicyId = policy.PolicyId,
                IssueDate = policy.StartDate,
                DueDate = policy.StartDate.AddDays(14),
                TotalNet = totalNet,
                VatAmount = vatAmount,
                TotalGross = totalNet + vatAmount,
                IsPaid = _faker.Random.Bool(0.85f) // 85% faktur opłaconych
            });
        }

        foreach (var batch in invoices.Chunk(1000))
        {
            await _context.Invoices.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation($"    ✓ Invoices: {invoices.Count}");
    }

    private async Task SeedCommissionsAsync(List<Policy> policies, List<Agent> agents)
    {
        var commissions = new List<Commission>();
        var statuses = await _context.CommissionStatuses.ToListAsync();
        var paidStatus = statuses.First(s => s.StatusName == "paid");
        var pendingStatus = statuses.First(s => s.StatusName == "pending");

        foreach (var policy in policies)
        {
            var agent = agents.First(a => a.AgentId == policy.AgentId);
            var commissionAmount = policy.PremiumAmount * (agent.CommissionRate / 100);

            commissions.Add(new Commission
            {
                PolicyId = policy.PolicyId,
                AgentId = agent.AgentId,
                CommissionRate = agent.CommissionRate,
                CommissionAmount = Math.Round(commissionAmount, 2),
                PaymentDate = _faker.Random.Bool(0.7f) ? policy.StartDate.AddDays(_faker.Random.Number(30, 90)) : null,
                CommissionStatusId = _faker.Random.Bool(0.7f) ? paidStatus.CommissionStatusId : pendingStatus.CommissionStatusId
            });
        }

        foreach (var batch in commissions.Chunk(1000))
        {
            await _context.Commissions.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation($"    ✓ Commissions: {commissions.Count}");
    }

    private async Task SeedRiskAssessmentsAsync(List<Policy> policies)
    {
        var assessments = new List<RiskAssessment>();
        var riskLevels = await _context.RiskLevels.ToListAsync();

        foreach (var policy in policies)
        {
            var riskLevel = _faker.PickRandom(riskLevels);

            assessments.Add(new RiskAssessment
            {
                PolicyId = policy.PolicyId,
                RiskLevelId = riskLevel.RiskLevelId,
                AssessmentDate = policy.StartDate.AddDays(-_faker.Random.Number(1, 7)),
                AssessedByUserId = _faker.Random.Number(1, 20),
                Score = _faker.Random.Decimal(0, 100),
                Notes = _faker.Lorem.Sentence()
            });
        }

        foreach (var batch in assessments.Chunk(1000))
        {
            await _context.RiskAssessments.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation($"    ✓ Risk Assessments: {assessments.Count}");
    }
}