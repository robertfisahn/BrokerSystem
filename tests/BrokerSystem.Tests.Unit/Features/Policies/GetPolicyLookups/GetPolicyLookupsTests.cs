using BrokerSystem.Api.Features.Policies.GetPolicyLookups;
using FluentAssertions;
using Xunit;

namespace BrokerSystem.Tests.Unit.Features.Policies.GetPolicyLookups;

public class GetPolicyLookupsTests
{
    public class ClientMock { public int Id { get; set; } public string? CompanyName { get; set; } public string? FirstName { get; set; } public string? LastName { get; set; } }
    public class AgentMock { public int Id { get; set; } public string? FirstName { get; set; } public string? LastName { get; set; } }

    [Fact]
    public void MapClient_WhenCompanyNameExists_ShouldReturnCompanyName()
    {
        // Arrange
        var client = new ClientMock { Id = 1, CompanyName = "Google", FirstName = "Larry", LastName = "Page" };

        // Act
        var result = LookupMapper.MapClient(client);

        // Assert
        result.Name.Should().Be("Google");
    }

    [Fact]
    public void MapClient_WhenNoCompanyName_ShouldReturnCombinedFirstAndLastName()
    {
        // Arrange
        var client = new ClientMock { Id = 2, CompanyName = null, FirstName = "Jan  ", LastName = " Kowalski" };

        // Act
        var result = LookupMapper.MapClient(client);

        // Assert
        result.Name.Should().Be("Jan Kowalski");
    }

    [Fact]
    public void MapClient_WhenAllNamesAreEmpty_ShouldReturnFallbackWithId()
    {
        // Arrange
        var client = new ClientMock { Id = 99, CompanyName = "", FirstName = null, LastName = " " };

        // Act
        var result = LookupMapper.MapClient(client);

        // Assert
        result.Name.Should().Be("Client #99");
    }

    [Fact]
    public void MapAgent_WhenNamesExist_ShouldReturnCombinedName()
    {
        // Arrange
        var agent = new AgentMock { Id = 5, FirstName = "Agent", LastName = "Smith" };

        // Act
        var result = LookupMapper.MapAgent(agent);

        // Assert
        result.Name.Should().Be("Agent Smith");
    }

    [Fact]
    public void MapAgent_WhenNamesAreEmpty_ShouldReturnFallbackWithId()
    {
        // Arrange
        var agent = new AgentMock { Id = 7, FirstName = "", LastName = null };

        // Act
        var result = LookupMapper.MapAgent(agent);

        // Assert
        result.Name.Should().Be("Agent #7");
    }
}
