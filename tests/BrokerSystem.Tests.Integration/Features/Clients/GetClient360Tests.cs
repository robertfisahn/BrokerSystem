using System.Net;
using BrokerSystem.Api.Features.Clients.GetClient360;
using BrokerSystem.Tests.Integration.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BrokerSystem.Tests.Integration.Features.Clients;

public class GetClient360Tests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetClient360_ShouldReturnFullProfile()
    {
        // Act
        var response = await _client.GetAsync("/api/clients/1/360");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<Client360Dto>();
        
        result.Should().NotBeNull();
        result!.ClientId.Should().Be(1);
        
        // Data verification from TestDataSeeder
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("User");
        
        // Relations
        result.Contacts.Should().NotBeEmpty();
        result.Contacts.Any(c => c.ContactValue == "test@user.com").Should().BeTrue();
        
        result.Addresses.Should().NotBeEmpty();
        result.Addresses.Any(a => a.City == "Warsaw").Should().BeTrue();
        
        result.Policies.Should().NotBeEmpty();
        var policy = result.Policies.First();
        policy.PolicyNumber.Should().Be("POL/TEST/001");
        
        // Nested Claims
        policy.Claims.Should().NotBeEmpty();
        policy.Claims.Any(c => c.ClaimNumber == "CLM/001").Should().BeTrue();
    }

    [Fact]
    public async Task GetClient360_WhenClientHasNoRelatedData_ShouldReturnEmptyCollections()
    {
        // Act - Client 2 (seeder adds it without contacts/addresses/policies)
        var response = await _client.GetAsync("/api/clients/2/360");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<Client360Dto>();
        
        result.Should().NotBeNull();
        result!.ClientId.Should().Be(2);
        
        // Assert empty collections (no crash/null)
        result.Contacts.Should().BeEmpty();
        result.Addresses.Should().BeEmpty();
        result.Policies.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClient360_WhenClientDoesNotExist_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/clients/999/360");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
