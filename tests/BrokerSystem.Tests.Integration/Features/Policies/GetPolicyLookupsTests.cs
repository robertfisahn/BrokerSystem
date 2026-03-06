using System.Net;
using BrokerSystem.Api.Features.Policies.GetPolicyLookups;
using BrokerSystem.Tests.Integration.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BrokerSystem.Tests.Integration.Features.Policies;

public class GetPolicyLookupsTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetLookups_ReturnsSeededDictionaryData()
    {
        // Act
        var response = await _client.GetAsync("/api/policies/lookups");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PolicyLookupsResponse>();

        result.Should().NotBeNull();
        
        // Check Clients, Types & Agents (Data from TestDataSeeder)
        result!.Clients.Should().NotBeEmpty();
        result.Clients.Should().Contain(c => c.Name.Contains("User"));
        
        result.PolicyTypes.Should().NotBeEmpty();
        result.PolicyTypes.Should().Contain(t => t.Name == "OC vehicle");

        result.Agents.Should().NotBeEmpty();
        result.Agents.Should().Contain(a => a.Name.Contains("Agent"));
    }
}
