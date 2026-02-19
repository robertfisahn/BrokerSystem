using BrokerSystem.Api.Features.Dashboard;
using BrokerSystem.Api.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace BrokerSystem.Tests.Unit.Features.Dashboard;

public class GetDashboardStatsTests
{
    [Fact]
    public void GetDashboardStatsSql_WhenUsingSqlServer_ShouldUseSqlServerFunctions()
    {
        // Arrange
        var dialect = new SqlServerDialect();

        // Act
        var sql = GetDashboardStatsHandler.GetDashboardStatsSql(dialect);

        // Assert
        sql.Should().Contain("YEAR(start_date)");
        sql.Should().Contain("MONTH(start_date)");
        sql.Should().Contain("CAST(YEAR(start_date) AS VARCHAR(4))");
    }

    [Fact]
    public void GetDashboardStatsSql_WhenUsingSqlite_ShouldUseSqliteFunctions()
    {
        // Arrange
        var dialect = new SqliteDialect();

        // Act
        var sql = GetDashboardStatsHandler.GetDashboardStatsSql(dialect);

        // Assert
        sql.Should().Contain("strftime('%Y', start_date)");
        sql.Should().Contain("strftime('%m', start_date)");
        sql.Should().Contain("strftime('%Y-%m', start_date)");
    }

    [Fact]
    public void GetDashboardStatsSql_ShouldContainAllMainSections()
    {
        // Arrange
        var dialect = new SqliteDialect();

        // Act
        var sql = GetDashboardStatsHandler.GetDashboardStatsSql(dialect);

        // Assert
        sql.Should().Contain("-- Monthly Sales");
        sql.Should().Contain("-- Client Type Distribution");
        sql.Should().Contain("-- Policy Status Distribution");
        sql.Should().Contain("-- KPIs");
    }

    [Theory]
    [InlineData("2024-02-15", "2023-03-01")]
    [InlineData("2024-01-01", "2023-02-01")]
    [InlineData("2023-12-31", "2023-01-01")]
    public void CalculateStartDateLimit_ShouldReturnTwelveMonthsAgo(string inputDate, string expectedDate)
    {
        // Act
        var result = GetDashboardStatsHandler.CalculateStartDateLimit(DateTime.Parse(inputDate));

        // Assert
        result.Should().Be(DateTime.Parse(expectedDate));
    }

    [Fact]
    public void Validator_ShouldBeValid()
    {
        // Arrange
        var validator = new GetDashboardStatsValidator();
        var query = new GetDashboardStatsQuery();

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
