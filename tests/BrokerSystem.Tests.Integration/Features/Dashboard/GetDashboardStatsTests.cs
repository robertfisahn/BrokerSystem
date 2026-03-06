using System.Net;
using BrokerSystem.Api.Features.Dashboard;
using BrokerSystem.Tests.Integration.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BrokerSystem.Tests.Integration.Features.Dashboard;

public class GetDashboardStatsTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetStats_ShouldReturnCompleteDashboardData()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<DashboardStatsResponse>();
        
        result.Should().NotBeNull();
        
        // 1. KPI verification
        result!.Kpis.Should().NotBeNull();
        result.Kpis.TotalClients.Should().BeGreaterThan(0);
        result.Kpis.TotalPolicies.Should().BeGreaterThan(0);
        result.Kpis.TotalPremiumVolume.Should().BeGreaterThan(0);

        // 2. Distributions
        result.ClientTypeDistribution.Should().NotBeEmpty();
        result.PolicyStatusDistribution.Should().NotBeEmpty();

        // 3. Sales Trend
        result.MonthlySales.Should().NotBeEmpty();
        // The first record should have the current month or previous months
        var latestMonth = result.MonthlySales.Last();
        latestMonth.Month.Should().NotBeNullOrWhiteSpace();
    }
}
