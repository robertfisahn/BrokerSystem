using Bogus;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Entities;
using BrokerSystem.Api.Infrastructure.Persistence.Seeding.Configuration;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Seeding.Seeders;

/// <summary>
/// Seeder dla klientów (WARSTWA 1)
/// Tworzy klientów B2C, B2B, VIP, Corporate wraz z adresami i kontaktami
/// </summary>
public class ClientSeeder
{
    private readonly BrokerSystemDbContext _context;
    private readonly ILogger _logger;
    private readonly Faker _faker = new("pl");

    public ClientSeeder(BrokerSystemDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(int totalClients = 2000)
    {
        if (await _context.Clients.AnyAsync())
        {
            _logger.LogInformation("  ⚠ Klienci już istnieją, pomijam...");
            return;
        }

        var startTime = DateTime.Now;

        // Pobierz typy klientów
        var b2cType = await _context.ClientTypes.FirstAsync(t => t.TypeName == "B2C");
        var b2bType = await _context.ClientTypes.FirstAsync(t => t.TypeName == "B2B");
        var vipType = await _context.ClientTypes.FirstAsync(t => t.TypeName == "VIP");
        var corporateType = await _context.ClientTypes.FirstAsync(t => t.TypeName == "Corporate");

        // Rozkład: 75% B2C, 15% B2B, 7.5% VIP, 2.5% Corporate
        int b2cCount = (int)(totalClients * 0.75);
        int b2bCount = (int)(totalClients * 0.15);
        int vipCount = (int)(totalClients * 0.075);
        int corporateCount = totalClients - b2cCount - b2bCount - vipCount;

        _logger.LogInformation($"  Generowanie {totalClients} klientów:");
        _logger.LogInformation($"    - B2C: {b2cCount}");
        _logger.LogInformation($"    - B2B: {b2bCount}");
        _logger.LogInformation($"    - VIP: {vipCount}");
        _logger.LogInformation($"    - Corporate: {corporateCount}");

        var allClients = new List<Client>();

        // B2C - osoby fizyczne
        var b2cClients = BogusConfiguration.GetPersonClientFaker(b2cType.ClientTypeId)
            .Generate(b2cCount);
        allClients.AddRange(b2cClients);

        // B2B - małe firmy
        var b2bClients = BogusConfiguration.GetCompanyClientFaker(b2bType.ClientTypeId)
            .Generate(b2bCount);
        allClients.AddRange(b2bClients);

        // VIP - osoby fizyczne premium
        var vipClients = BogusConfiguration.GetPersonClientFaker(vipType.ClientTypeId)
            .Generate(vipCount);
        allClients.AddRange(vipClients);

        // Corporate - duże firmy
        var corporateClients = BogusConfiguration.GetCompanyClientFaker(corporateType.ClientTypeId)
            .Generate(corporateCount);
        allClients.AddRange(corporateClients);

        // Zapisz klientów (w batch'ach po 1000)
        int saved = 0;
        foreach (var batch in allClients.Chunk(1000))
        {
            await _context.Clients.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
            saved += batch.Length;
            _logger.LogInformation($"    Zapisano {saved}/{totalClients} klientów...");
        }

        // Dodaj adresy (avg 1.5 adresu/klient)
        await SeedClientAddressesAsync(allClients);

        // Dodaj kontakty (avg 2 kontakty/klient)
        await SeedClientContactsAsync(allClients);

        var duration = (DateTime.Now - startTime).TotalSeconds;
        _logger.LogInformation($"  ✓ Klienci: {totalClients} ({duration:F1}s)");
    }

    private async Task SeedClientAddressesAsync(List<Client> clients)
    {
        var addresses = new List<ClientAddress>();

        foreach (var client in clients)
        {
            // Każdy klient ma przynajmniej 1 adres
            int addressCount = _faker.Random.WeightedRandom(
                new[] { 1, 2, 3 },
                new[] { 0.6f, 0.3f, 0.1f } // 60% ma 1, 30% ma 2, 10% ma 3
            );

            for (int i = 0; i < addressCount; i++)
            {
                var address = BogusConfiguration.GetClientAddressFaker(client.ClientId).Generate();

                // Tylko pierwszy adres jest current
                address.IsCurrent = i == 0;

                // Starsze adresy mają valid_to
                if (i > 0)
                {
                    // Start min. 2 lata temu, max rok temu
                    address.ValidFrom = DateOnly.FromDateTime(_faker.Date.Between(DateTime.Now.AddYears(-5), DateTime.Now.AddYears(-2)));
                    // Koniec min. 30 dni po starcie, max rok po starcie
                    address.ValidTo = address.ValidFrom.AddDays(_faker.Random.Number(30, 360));
                }

                addresses.Add(address);
            }
        }

        foreach (var batch in addresses.Chunk(1000))
        {
            await _context.ClientAddresses.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation($"    ✓ Adresy: {addresses.Count}");
    }

    private async Task SeedClientContactsAsync(List<Client> clients)
    {
        var contacts = new List<ClientContact>();

        foreach (var client in clients)
        {
            // Email (primary)
            var emailContact = BogusConfiguration.GetClientContactFaker(client.ClientId).Generate();
            emailContact.ContactType = "email";
            emailContact.ContactValue = _faker.Internet.Email(
                client.FirstName ?? "firma",
                client.LastName ?? client.CompanyName
            );
            emailContact.IsPrimary = true;
            contacts.Add(emailContact);

            // Telefon (primary dla 70% klientów)
            if (_faker.Random.Bool(0.9f)) // 90% ma telefon
            {
                var phoneContact = BogusConfiguration.GetClientContactFaker(client.ClientId).Generate();
                phoneContact.ContactType = "mobile";
                phoneContact.ContactValue = $"+48 {_faker.Random.Number(500, 799)} {_faker.Random.Number(100, 999)} {_faker.Random.Number(100, 999)}";
                phoneContact.IsPrimary = _faker.Random.Bool(0.7f);
                contacts.Add(phoneContact);
            }

            // Dodatkowy kontakt (20% klientów)
            if (_faker.Random.Bool(0.2f))
            {
                var extraContact = BogusConfiguration.GetClientContactFaker(client.ClientId).Generate();
                extraContact.ContactType = _faker.PickRandom("landline", "mobile");
                extraContact.ContactValue = $"+48 {_faker.Random.Number(200, 799)} {_faker.Random.Number(100, 999)} {_faker.Random.Number(100, 999)}";
                extraContact.IsPrimary = false;
                contacts.Add(extraContact);
            }
        }

        foreach (var batch in contacts.Chunk(1000))
        {
            await _context.ClientContacts.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation($"    ✓ Kontakty: {contacts.Count}");
    }
}