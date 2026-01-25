using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure.Persistence.Seeding.Seeders;
using Microsoft.EntityFrameworkCore;

namespace BrokerSystem.Api.Infrastructure.Persistence.Seeding;

/// <summary>
/// Główny orchestrator seedowania bazy danych
/// Wykonuje seedowanie w odpowiedniej kolejności (warstwy zależności)
/// </summary>
public class DatabaseSeeder
{
    private readonly BrokerSystemDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(BrokerSystemDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Główna metoda seedująca - wykonuje wszystko w odpowiedniej kolejności
    /// </summary>
    public async Task SeedAllAsync(bool resetDatabase = false)
    {
        _logger.LogInformation("=== ROZPOCZĘCIE SEEDOWANIA BAZY DANYCH ===");
        var startTime = DateTime.Now;

        try
        {
            // Opcjonalnie: wyczyść bazę
            if (resetDatabase)
            {
                _logger.LogWarning("UWAGA: Czyszczenie bazy danych...");
                await ClearDatabaseAsync();
            }

            // WARSTWA 0: Słowniki (zero zależności)
            _logger.LogInformation("WARSTWA 0: Seedowanie słowników...");
            var dictionarySeeder = new DictionarySeeder(_context, _logger);
            await dictionarySeeder.SeedAsync();

            // WARSTWA 1: Klienci
            _logger.LogInformation("WARSTWA 1: Seedowanie klientów...");
            var clientSeeder = new ClientSeeder(_context, _logger);
            await clientSeeder.SeedAsync(2000); // 2000 klientów

            // WARSTWA 2: Agenci (hierarchia!)
            _logger.LogInformation("WARSTWA 2: Seedowanie agentów (hierarchia)...");
            var agentSeeder = new AgentSeeder(_context, _logger);
            await agentSeeder.SeedAsync(100); // 100 agentów

            // WARSTWA 3: Polisy (GŁÓWNA TABELA)
            _logger.LogInformation("WARSTWA 3: Seedowanie polis...");
            var policySeeder = new PolicySeeder(_context, _logger);
            await policySeeder.SeedAsync(5000); // 5000 polis

            // WARSTWA 4: Roszczenia
            _logger.LogInformation("WARSTWA 4: Seedowanie roszczeń...");
            var claimSeeder = new ClaimSeeder(_context, _logger);
            await claimSeeder.SeedAsync(800); // 800 szkód

            // WARSTWA 5: Finanse
            _logger.LogInformation("WARSTWA 5: Seedowanie finansów...");
            var financialSeeder = new FinancialSeeder(_context, _logger);
            await financialSeeder.SeedAsync();

            // Raport końcowy
            var duration = DateTime.Now - startTime;
            _logger.LogInformation("=== SEEDOWANIE ZAKOŃCZONE POMYŚLNIE ===");
            _logger.LogInformation($"Czas wykonania: {duration.TotalSeconds:F2} sekund");
            await LogDatabaseStatisticsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BŁĄD podczas seedowania bazy danych!");
            throw;
        }
    }

    /// <summary>
    /// Czyści całą bazę danych (DELETE wszystkich rekordów)
    /// UWAGA: Tylko do developmentu!
    /// </summary>
    private async Task ClearDatabaseAsync()
    {
        await _context.Database.ExecuteSqlRawAsync("EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
        await _context.Database.ExecuteSqlRawAsync("EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; DELETE FROM ?'");
        await _context.Database.ExecuteSqlRawAsync("EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; ALTER TABLE ? CHECK CONSTRAINT ALL'");

        // Resetuj IDENTITY
        await _context.Database.ExecuteSqlRawAsync(@"
            EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; IF OBJECTPROPERTY(OBJECT_ID(''?''), ''TableHasIdentity'') = 1 
            DBCC CHECKIDENT (''?'', RESEED, 0)'
        ");

        _logger.LogWarning("Baza danych wyczyszczona!");
    }

    /// <summary>
    /// Wyświetla statystyki bazy danych po seedowaniu
    /// </summary>
    private async Task LogDatabaseStatisticsAsync()
    {
        _logger.LogInformation("--- STATYSTYKI BAZY DANYCH ---");
        _logger.LogInformation($"Klienci: {await _context.Clients.CountAsync()}");
        _logger.LogInformation($"  - B2C: {await _context.Clients.CountAsync(c => c.ClientType.TypeName == "B2C")}");
        _logger.LogInformation($"  - B2B: {await _context.Clients.CountAsync(c => c.ClientType.TypeName == "B2B")}");
        _logger.LogInformation($"  - VIP: {await _context.Clients.CountAsync(c => c.ClientType.TypeName == "VIP")}");
        _logger.LogInformation($"  - Corporate: {await _context.Clients.CountAsync(c => c.ClientType.TypeName == "Corporate")}");

        _logger.LogInformation($"Adresy: {await _context.ClientAddresses.CountAsync()}");
        _logger.LogInformation($"Kontakty: {await _context.ClientContacts.CountAsync()}");
        _logger.LogInformation($"Agenci: {await _context.Agents.CountAsync()}");
        _logger.LogInformation($"Polisy: {await _context.Policies.CountAsync()}");
        _logger.LogInformation($"  - Aktywne: {await _context.Policies.CountAsync(p => p.Status.IsActivePolicy)}");
        _logger.LogInformation($"Roszczenia: {await _context.Claims.CountAsync()}");
        _logger.LogInformation($"Płatności: {await _context.Payments.CountAsync()}");
        _logger.LogInformation($"Faktury: {await _context.Invoices.CountAsync()}");
        _logger.LogInformation($"Prowizje: {await _context.Commissions.CountAsync()}");
        _logger.LogInformation($"Użytkownicy: {await _context.Users.CountAsync()}");
        _logger.LogInformation("-------------------------------");
    }
}