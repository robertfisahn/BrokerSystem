using BrokerSystem.Api.Features.Policies.ExportPolicy;
using BrokerSystem.Api.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace BrokerSystem.Tests.Unit.Features.Policies.ExportPolicy;

public class ExportPolicyTests
{
    [Fact]
    public void GetExportSql_WhenUsingSqlServer_ShouldUsePlusForConcatenation()
    {
        // Arrange
        var dialect = new SqlServerDialect();

        // Act
        var sql = ExportPolicyHandler.GetExportSql(dialect);

        // Assert
        sql.Should().Contain("a.first_name + ' ' + a.last_name");
        sql.Should().Contain("WHERE p.policy_id = @PolicyId");
    }

    [Fact]
    public void GetExportSql_WhenUsingSqlite_ShouldUsePipesForConcatenation()
    {
        // Arrange
        var dialect = new SqliteDialect();

        // Act
        var sql = ExportPolicyHandler.GetExportSql(dialect);

        // Assert
        sql.Should().Contain("a.first_name || ' ' || a.last_name");
    }

    [Fact]
    public void GetExportSql_ShouldContainAllNecessaryJoins()
    {
        // Arrange
        var dialect = new SqliteDialect();

        // Act
        var sql = ExportPolicyHandler.GetExportSql(dialect);

        // Assert
        sql.Should().Contain("JOIN clients c");
        sql.Should().Contain("JOIN policy_types pt");
        sql.Should().Contain("JOIN policy_statuses ps");
        sql.Should().Contain("JOIN agents a");
    }
}
