using System.Net;
using BrokerSystem.Api.Features.Policies.GetPolicies;
using BrokerSystem.Tests.Integration.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BrokerSystem.Tests.Integration.Features.Policies;

public class GetPoliciesTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetPolicies_WithNoFilters_ReturnsDefaultPaginatedData()
    {
        // Act
        var response = await _client.GetAsync("/api/policies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedPoliciesResponse>();
        
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetPolicies_WithPagination_ReturnsCorrectSubset()
    {
        // Act
        var response = await _client.GetAsync("/api/policies?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedPoliciesResponse>();
        
        result!.Items.Count.Should().BeLessOrEqualTo(2);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetPolicies_WithMultiWordSearch_ReturnsMatchingPolicies()
    {
        // Arrange
        var search = "POL/TEST User";

        // Act
        var response = await _client.GetAsync($"/api/policies?search={search}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedPoliciesResponse>();
        
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(p => 
        {
            (p.PolicyNumber.Contains("POL/TEST") || p.ClientName.Contains("User")).Should().BeTrue();
        });
    }

    [Fact]
    public async Task GetPolicies_WithSorting_ByTotalPremium_ReturnsOrderedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/policies?sortBy=totalpremium&sortDescending=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedPoliciesResponse>();
        
        result!.Items.Should().BeInDescendingOrder(p => p.TotalPremium);
    }

    [Fact]
    public async Task GetPolicies_WithInvalidPage_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/policies?page=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPolicies_WithWhitespaceSearch_ReturnsAllData()
    {
        // Act
        var response = await _client.GetAsync("/api/policies?search=   ");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedPoliciesResponse>();
        result!.Items.Should().NotBeEmpty();
    }
}
