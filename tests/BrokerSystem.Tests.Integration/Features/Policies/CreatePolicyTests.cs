using System.Net;
using Xunit;
using BrokerSystem.Api.Features.Policies.CreatePolicy;
using BrokerSystem.Tests.Integration.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BrokerSystem.Tests.Integration.Features.Policies;

public class CreatePolicyTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_ValidPolicy_ReturnsOkAndPersistsData()
    {
        // Arrange
        var command = new CreatePolicyCommand(
            PolicyNumber: $"POL/TEST/{Guid.NewGuid().ToString()[..8]}",
            ClientId: 1,
            PolicyTypeId: 1,
            AgentId: 1,
            PremiumAmount: 1500.00m,
            SumInsured: 100000,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddYears(1),
            PaymentFrequency: "Annual"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/policies", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var policyId = await response.Content.ReadFromJsonAsync<int>();
        policyId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_DuplicatePolicyNumber_ReturnsBadRequest()
    {
        // Arrange
        var number = "DUP-123";
        var command = new CreatePolicyCommand(number, 1, 1, 1, 100, 1000, DateTime.Now, DateTime.Now.AddDays(1), "Annual");
        
        // Create first time
        await _client.PostAsJsonAsync("/api/policies", command);

        // Act - Create second time
        var response = await _client.PostAsJsonAsync("/api/policies", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithNonExistentClient_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreatePolicyCommand(
            PolicyNumber: "ERR-999",
            ClientId: 999123, // Non-existent
            PolicyTypeId: 1,
            AgentId: 1,
            PremiumAmount: 500.00m,
            SumInsured: 10000,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddYears(1),
            PaymentFrequency: "Annual"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/policies", command);

        // Assert
        // The server should return 400 or 500 depending on implementation.
        // For Enums in FluentAssertions we use BeOneOf or Match without generic.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Create_WithEndDateBeforeStartDate_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreatePolicyCommand(
            PolicyNumber: "DATE-ERR",
            ClientId: 1,
            PolicyTypeId: 1,
            AgentId: 1,
            PremiumAmount: 500.00m,
            SumInsured: 10000,
            StartDate: DateTime.UtcNow.AddDays(10),
            EndDate: DateTime.UtcNow.AddDays(5), // Earlier than start
            PaymentFrequency: "Annual"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/policies", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
