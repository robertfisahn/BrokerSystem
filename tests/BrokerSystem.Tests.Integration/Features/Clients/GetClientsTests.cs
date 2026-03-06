using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using BrokerSystem.Tests.Integration.Helpers;
using Xunit;
using BrokerSystem.Api.Common.Models;
using BrokerSystem.Api.Features.Clients.GetClients;

namespace BrokerSystem.Tests.Integration.Features.Clients;

public class GetClientsTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Get_Clients_ShouldReturnPaginatedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/clients?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<GetClientsDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterOrEqualTo(5); // We seeded 5 clients
    }

    [Fact]
    public async Task Get_Clients_WithCitySearch_ShouldReturnFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/clients?search=Krakow");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<GetClientsDto>>();
        result!.Items.Should().ContainSingle(c => c.City == "Krakow");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_Clients_WithMultiWordSearch_ShouldReturnFilteredResults()
    {
        // Act - Warsaw and User (matches Client 1: Warsaw, Firstname: Test, Lastname: User)
        var response = await _client.GetAsync("/api/clients?search=Warsaw User");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<GetClientsDto>>();
        result!.Items.Should().ContainSingle(c => c.LastName == "User" && c.City == "Warsaw");
    }

    [Fact]
    public async Task Get_Clients_WithSorting_ByActivePolicies_ShouldWork()
    {
        // Act
        var response = await _client.GetAsync("/api/clients?sortBy=activePoliciesCount&sortDescending=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<GetClientsDto>>();
        // Client 1 has 1 active policy, others have 0
        result!.Items.First().ActivePoliciesCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_Clients_WithInvalidPage_ShouldReturn400()
    {
        // Act
        var response = await _client.GetAsync("/api/clients?page=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
