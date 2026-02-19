using BrokerSystem.Api.Features.Clients.GetClientsStats;
using FluentAssertions;
using Xunit;

namespace BrokerSystem.Tests.Unit.Features.Clients.GetClientsStats;

public class GetClientsStatsTests
{
    [Fact]
    public void GetStatsSql_ShouldContainCorrectFilters()
    {
        // Arrange
        var sql = GetClientsStatsHandler.GetStatsSql;

        // Assert
        sql.Should().Contain("ct.type_name = 'VIP'");
        sql.Should().Contain("ct.type_name = 'Corporate'");
        sql.Should().Contain("ps.is_active_policy = 1");
        sql.Should().Contain("registration_date >= @StartOfMonth");
    }

    [Fact]
    public void GetStatsSql_ShouldReturnAllExpectedColumns()
    {
        // Arrange
        var sql = GetClientsStatsHandler.GetStatsSql;

        // Assert
        sql.Should().Contain("as TotalClients");
        sql.Should().Contain("as VipClients");
        sql.Should().Contain("as CorporateClients");
        sql.Should().Contain("as ActivePoliciesTotal");
        sql.Should().Contain("as NewClientsThisMonth");
    }

    [Theory]
    [InlineData("2024-02-15", "2024-02-01")]
    [InlineData("2024-01-01", "2024-01-01")]
    [InlineData("2023-12-31", "2023-12-01")]
    public void CalculateStartOfMonth_ShouldReturnFirstDay(string inputDate, string expectedDate)
    {
        // Act
        var result = GetClientsStatsHandler.CalculateStartOfMonth(DateTime.Parse(inputDate));

        // Assert
        result.Should().Be(DateTime.Parse(expectedDate));
    }

    [Fact]
    public void MapResult_WhenNull_ShouldReturnEmptyDto()
    {
        // Act
        var result = GetClientsStatsHandler.MapResult(null);

        // Assert
        result.Should().NotBeNull();
        result.TotalClients.Should().Be(0);
    }
}
