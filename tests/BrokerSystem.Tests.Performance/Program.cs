using BenchmarkDotNet.Running;
using BrokerSystem.Tests.Performance.Benchmarks.Clients;

Console.WriteLine("=== BrokerSystem Performance Laboratory: Focus GetClients ===");

BenchmarkRunner.Run<GetClientsBenchmarks>();
