using BenchmarkDotNet.Running;
using BrokerSystem.Tests.Performance.Benchmarks;
using BrokerSystem.Tests.Performance.Benchmarks.Clients;
using BrokerSystem.Tests.Performance.Benchmarks.Dashboard;

Console.WriteLine("=== BrokerSystem Performance Laboratory ===");
Console.WriteLine("Wybierz zestaw testowy do uruchomienia:");
Console.WriteLine("1. GetClients (Lista klientów - Baseline vs Dapper)");
Console.WriteLine("2. Client360 (Profil 360 - Heavy Includes vs Dapper Hybrid)");
Console.WriteLine("3. Dashboard (Statystyki - EF vs Dapper Raw vs Procedure)");
Console.WriteLine("4. ReadSide, Features Refactor - Dapper vs EF");
Console.WriteLine("");
Console.WriteLine("Wskazówka: Możesz wpisać '1', '2', '3' lub '4', albo '*' aby uruchomić wszystkie.");

var switcher = new BenchmarkSwitcher(new[] {
    typeof(GetClientsBenchmarks),
    typeof(Client360Benchmarks),
    typeof(DashboardBenchmarks),
    typeof(ReadSideBenchmarks)
});

switcher.Run(args);
