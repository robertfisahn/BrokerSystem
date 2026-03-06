using System.Net;
using BrokerSystem.Api.Features.Clients.GetClientsStats;
using BrokerSystem.Tests.Integration.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BrokerSystem.Tests.Integration.Features.Clients;

public class GetClientsStatsTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetStats_ShouldReturnCorrectCalculations()
    {
        // Act
        var response = await _client.GetAsync("/api/clients/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var stats = await response.Content.ReadFromJsonAsync<ClientsStatsDto>();
        stats.Should().NotBeNull();
        
        // Expectations based on TestDataSeeder:
        // Client 1: Individual, Today
        // Client 2: Corporate, Today
        // Client 3: VIP, Today
        // Client 4: Individual, Last Month
        // Total = 5
        // VIP = 1
        // Corporate = 1
        // New (This Month) = 4 (Krakow User is today)
        // Active Policies = 1 (Seeded in TestDataSeeder)

        stats!.TotalClients.Should().Be(5);
        stats.VipClients.Should().Be(1);
        stats.CorporateClients.Should().Be(1);
        stats.NewClientsThisMonth.Should().Be(4);
        stats.ActivePoliciesTotal.Should().Be(1);
    }
}
