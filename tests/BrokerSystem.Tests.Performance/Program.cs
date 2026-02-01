using BenchmarkDotNet.Running;
using BrokerSystem.Tests.Performance.Benchmarks.Clients;
using BrokerSystem.Tests.Performance.Benchmarks.Dashboard;

Console.WriteLine("=== BrokerSystem Performance Laboratory ===");
Console.WriteLine("Wybierz zestaw testowy do uruchomienia:");
Console.WriteLine("1. GetClients (Lista klientów - Baseline vs Dapper)");
Console.WriteLine("2. Client360 (Profil 360 - Heavy Includes vs Dapper Hybrid)");
Console.WriteLine("3. Dashboard (Statystyki - EF vs Dapper Raw vs Procedure)");
Console.WriteLine("");
Console.WriteLine("Wskazówka: Możesz wpisać '1', '2' lub '3', albo '*' aby uruchomić wszystkie.");

var switcher = new BenchmarkSwitcher(new[] {
    typeof(GetClientsBenchmarks),
    typeof(Client360Benchmarks),
    typeof(DashboardBenchmarks)
});

switcher.Run(args);
