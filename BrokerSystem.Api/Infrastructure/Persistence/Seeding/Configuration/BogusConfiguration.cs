using Bogus;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;

namespace BrokerSystem.Api.Infrastructure.Persistence.Seeding.Configuration;

/// <summary>
/// Konfiguracje Bogus Faker dla wszystkich encji
/// Realistyczne dane z polską lokalizacją
/// </summary>
public static class BogusConfiguration
{
    private static readonly string[] PolishCities = new[]
    {
        "Warszawa", "Kraków", "Wrocław", "Poznań", "Gdańsk",
        "Szczecin", "Bydgoszcz", "Lublin", "Katowice", "Białystok"
    };

    private static readonly string[] PolishStreets = new[]
    {
        "Marszałkowska", "Długa", "Krótka", "Słoneczna", "Kwiatowa",
        "Ogrodowa", "Polna", "Leśna", "Spacerowa", "Parkowa"
    };

    private static int _policyCounter = 100000;
    private static int _claimCounter = 100000;
    private static int _invoiceCounter = 100000;
    private static int _transactionCounter = 100000;

    public static string NextPolicyNumber() => $"POL/{DateTime.Now.Year}/{++_policyCounter}";
    public static string NextClaimNumber() => $"CLM/{DateTime.Now.Year}/{++_claimCounter}";
    public static string NextInvoiceNumber() => $"INV/{DateTime.Now.Year}/{++_invoiceCounter}";
    public static string NextTransactionId() => $"TRX/{DateTime.Now.Year}{DateTime.Now.Month:D2}/{++_transactionCounter}";

    /// <summary>
    /// Faker dla klientów indywidualnych (B2C, VIP)
    /// </summary>
    public static Faker<Client> GetPersonClientFaker(int clientTypeId)
    {
        return new Faker<Client>("pl")
            .RuleFor(c => c.ClientTypeId, clientTypeId)
            .RuleFor(c => c.FirstName, f => f.Name.FirstName())
            .RuleFor(c => c.LastName, f => f.Name.LastName())
            .RuleFor(c => c.CompanyName, _ => null)
            .RuleFor(c => c.TaxId, f => GeneratePesel(f))
            .RuleFor(c => c.DateOfBirth, f => DateOnly.FromDateTime(f.Date.Between(
                DateTime.Now.AddYears(-70),
                DateTime.Now.AddYears(-18)
            )))
            .RuleFor(c => c.RegistrationDate, (f, c) => DateOnly.FromDateTime(f.Date.Between(
                c.DateOfBirth!.Value.ToDateTime(TimeOnly.MinValue).AddYears(18),
                DateTime.Now
            )))
            .RuleFor(c => c.IsActive, f => f.Random.Bool(0.95f)) // 95% aktywnych
            .RuleFor(c => c.RiskScore, f => f.Random.Decimal(10, 90))
            .RuleFor(c => c.CreatedAt, (f, c) => c.RegistrationDate.ToDateTime(TimeOnly.MinValue))
            .RuleFor(c => c.UpdatedAt, (f, c) => c.RegistrationDate.ToDateTime(TimeOnly.MinValue));
    }

    /// <summary>
    /// Faker dla klientów biznesowych (B2B, Corporate)
    /// </summary>
    public static Faker<Client> GetCompanyClientFaker(int clientTypeId)
    {
        return new Faker<Client>("pl")
            .RuleFor(c => c.ClientTypeId, clientTypeId)
            .RuleFor(c => c.FirstName, _ => null)
            .RuleFor(c => c.LastName, _ => null)
            .RuleFor(c => c.CompanyName, f => f.Company.CompanyName())
            .RuleFor(c => c.TaxId, f => GenerateNip(f))
            .RuleFor(c => c.DateOfBirth, _ => null)
            .RuleFor(c => c.RegistrationDate, f => DateOnly.FromDateTime(f.Date.Between(
                DateTime.Now.AddYears(-10),
                DateTime.Now
            )))
            .RuleFor(c => c.IsActive, f => f.Random.Bool(0.97f))
            .RuleFor(c => c.RiskScore, f => f.Random.Decimal(15, 85))
            .RuleFor(c => c.CreatedAt, (f, c) => c.RegistrationDate.ToDateTime(TimeOnly.MinValue))
            .RuleFor(c => c.UpdatedAt, (f, c) => c.RegistrationDate.ToDateTime(TimeOnly.MinValue));
    }

    /// <summary>
    /// Faker dla adresów klientów
    /// </summary>
    public static Faker<ClientAddress> GetClientAddressFaker(int clientId)
    {
        return new Faker<ClientAddress>("pl")
            .RuleFor(a => a.ClientId, clientId)
            .RuleFor(a => a.AddressType, f => f.PickRandom("home", "work", "billing"))
            .RuleFor(a => a.Street, f => $"{f.PickRandom(PolishStreets)} {f.Random.Number(1, 200)}")
            .RuleFor(a => a.City, f => f.PickRandom(PolishCities))
            .RuleFor(a => a.PostalCode, f => $"{f.Random.Number(10, 99)}-{f.Random.Number(100, 999)}")
            .RuleFor(a => a.Country, "Poland")
            .RuleFor(a => a.ValidFrom, f => DateOnly.FromDateTime(f.Date.Past(3)))
            .RuleFor(a => a.ValidTo, _ => (DateOnly?)null)
            .RuleFor(a => a.IsCurrent, true);
    }

    /// <summary>
    /// Faker dla kontaktów klientów
    /// </summary>
    public static Faker<ClientContact> GetClientContactFaker(int clientId)
    {
        return new Faker<ClientContact>("pl")
            .RuleFor(c => c.ClientId, clientId)
            .RuleFor(c => c.ContactType, f => f.PickRandom("email", "mobile"))
            .RuleFor(c => c.ContactValue, (f, c) =>
                c.ContactType == "email"
                    ? f.Internet.Email()
                    : $"+48 {f.Random.Number(500, 799)} {f.Random.Number(100, 999)} {f.Random.Number(100, 999)}"
            )
            .RuleFor(c => c.IsPrimary, f => f.Random.Bool(0.7f))
            .RuleFor(c => c.VerifiedAt, f => f.Random.Bool(0.8f) ? f.Date.Recent(30) : null);
    }

    /// <summary>
    /// Faker dla agentów
    /// </summary>
    public static Faker<Agent> GetAgentFaker(int? managerId = null)
    {
        return new Faker<Agent>("pl")
            .RuleFor(a => a.FirstName, f => f.Name.FirstName())
            .RuleFor(a => a.LastName, f => f.Name.LastName())
            .RuleFor(a => a.Email, (f, a) =>
                $"{a.FirstName}.{a.LastName}@brokersystem.pl".ToLower()
                    .Replace("ł", "l")
                    .Replace("ą", "a")
                    .Replace("ć", "c")
                    .Replace("ę", "e")
                    .Replace("ń", "n")
                    .Replace("ó", "o")
                    .Replace("ś", "s")
                    .Replace("ź", "z")
                    .Replace("ż", "z")
            )
            .RuleFor(a => a.Phone, f =>
                $"+48 {f.Random.Number(600, 799)} {f.Random.Number(100, 999)} {f.Random.Number(100, 999)}"
            )
            .RuleFor(a => a.ManagerId, managerId)
            .RuleFor(a => a.HireDate, f => DateOnly.FromDateTime(f.Date.Between(DateTime.Now.AddYears(-10), DateTime.Now.AddMonths(-1))))
            .RuleFor(a => a.CommissionRate, f => f.Random.Decimal(5, 20))
            .RuleFor(a => a.IsActive, f => f.Random.Bool(0.95f));
    }

    /// <summary>
    /// Faker dla polis
    /// </summary>
    public static Faker<Policy> GetPolicyFaker(
        int clientId,
        int policyTypeId,
        int agentId,
        int statusId,
        decimal basePremium)
    {
        return new Faker<Policy>("pl")
            .RuleFor(p => p.PolicyNumber, _ => NextPolicyNumber())
            .RuleFor(p => p.ClientId, clientId)
            .RuleFor(p => p.PolicyTypeId, policyTypeId)
            .RuleFor(p => p.AgentId, agentId)
            .RuleFor(p => p.StatusId, statusId)
            .RuleFor(p => p.StartDate, f => DateOnly.FromDateTime(f.Date.Between(
                DateTime.Now.AddYears(-3),
                DateTime.Now
            )))
            .RuleFor(p => p.EndDate, (f, p) => p.StartDate.AddYears(1))
            .RuleFor(p => p.PremiumAmount, f =>
                Math.Round(basePremium * f.Random.Decimal(0.8m, 1.3m), 2)
            )
            .RuleFor(p => p.SumInsured, (f, p) =>
                Math.Round(p.PremiumAmount * f.Random.Number(20, 100), 2)
            )
            .RuleFor(p => p.PaymentFrequency, f =>
                f.PickRandom("monthly", "quarterly", "annual")
            )
            .RuleFor(p => p.CreatedAt, (f, p) => p.StartDate.ToDateTime(TimeOnly.MinValue))
            .RuleFor(p => p.UpdatedAt, (f, p) => p.StartDate.ToDateTime(TimeOnly.MinValue));
    }

    /// <summary>
    /// Faker dla szkód
    /// </summary>
    public static Faker<Claim> GetClaimFaker(int policyId, Policy policy, int statusId)
    {
        return new Faker<Claim>("pl")
            .RuleFor(c => c.ClaimNumber, _ => NextClaimNumber())
            .RuleFor(c => c.PolicyId, policyId)
            .RuleFor(c => c.StatusId, statusId)
            .RuleFor(c => c.IncidentDate, f => DateOnly.FromDateTime(f.Date.Between(policy.StartDate.ToDateTime(TimeOnly.MinValue), DateTime.Now)))
            .RuleFor(c => c.ReportedDate, (f, c) => DateOnly.FromDateTime(f.Date.Between(c.IncidentDate.ToDateTime(TimeOnly.MinValue), DateTime.Now)))
            .RuleFor(c => c.ClaimedAmount, f => f.Random.Decimal(500, 50000))
            .RuleFor(c => c.ApprovedAmount, (f, c) =>
                f.Random.Bool(0.8f) ? Math.Round(c.ClaimedAmount * f.Random.Decimal(0.7m, 1.0m), 2) : (decimal?)null
            )
            .RuleFor(c => c.Description, f => f.Lorem.Sentence(10))
            .RuleFor(c => c.CreatedAt, (f, c) => c.ReportedDate.ToDateTime(TimeOnly.MinValue));
    }

    /// <summary>
    /// Generuje realistyczny PESEL
    /// </summary>
    private static string GeneratePesel(Faker f)
    {
        return $"{f.Random.Number(10, 99)}{f.Random.Number(10, 12):D2}{f.Random.Number(1, 28):D2}{f.Random.Number(10000, 99999)}";
    }

    /// <summary>
    /// Generuje realistyczny NIP
    /// </summary>
    private static string GenerateNip(Faker f)
    {
        return $"{f.Random.Number(100, 999)}-{f.Random.Number(100, 999)}-{f.Random.Number(10, 99)}-{f.Random.Number(10, 99)}";
    }
}