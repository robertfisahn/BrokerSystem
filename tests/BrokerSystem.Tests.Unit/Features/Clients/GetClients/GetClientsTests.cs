using BrokerSystem.Api.Features.Clients.GetClients;
using BrokerSystem.Api.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace BrokerSystem.Tests.Unit.Features.Clients.GetClients;

public class GetClientsTests
{
    private readonly GetClientsValidator _validator = new();

    [Fact]
    public void GetMainSql_ShouldContainRequiredColumnsAndJoins()
    {
        // Arrange
        var sqlDialect = new SqlServerDialect();
        var where = "WHERE 1=1";
        var orderBy = "c.client_id ASC";

        // Act
        var sql = GetClientsHandler.GetMainSql(sqlDialect, where, orderBy);

        // Assert
        sql.Should().Contain("c.client_id AS ClientId");
        sql.Should().Contain("ct.type_name AS ClientType");
        sql.Should().Contain("JOIN client_types ct");
        sql.Should().Contain("PrimaryContact");
        sql.Should().Contain("City");
        sql.Should().Contain("ActivePoliciesCount");
    }

    [Theory]
    [InlineData(0, 20, "clientId", false)]
    [InlineData(1, 0, "clientId", false)]
    [InlineData(1, 101, "clientId", false)]
    [InlineData(1, 20, "", false)]
    public void Validator_WithInvalidData_ShouldHaveErrors(int page, int pageSize, string sortBy, bool descending)
    {
        // Arrange
        var query = new GetClientsQuery(page, pageSize, null, sortBy, descending);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BuildFilterQuery_WhenSearchIsEmpty_ShouldReturnDefaultWhereClause()
    {
        // Act
        var (whereClause, parameters) = GetClientsHandler.BuildFilterQuery("");

        // Assert
        whereClause.Should().Be("WHERE 1=1");
        parameters.ParameterNames.Should().BeEmpty();
    }

    [Theory]
    [InlineData("John Google", 2, new[] { "%John%", "%Google%" })]
    [InlineData("  Smith  ", 1, new[] { "%Smith%" })]
    [InlineData(null, 0, new string[] { })]
    [InlineData("   ", 0, new string[] { })]
    public void BuildFilterQuery_SearchScenarios_ShouldCreateCorrectParameters(string? search, int expectedCount, string[] expectedValues)
    {
        // Act
        var (whereClause, parameters) = GetClientsHandler.BuildFilterQuery(search);

        // Assert
        parameters.ParameterNames.Should().HaveCount(expectedCount);
        for (int i = 0; i < expectedCount; i++)
        {
            parameters.Get<string>($"@p{i}").Should().Be(expectedValues[i]);
            whereClause.Should().Contain($"c.first_name LIKE @p{i}");
            whereClause.Should().Contain($"EXISTS (SELECT 1 FROM client_addresses ca WHERE ca.client_id = c.client_id AND ca.city LIKE @p{i})");
        }
    }

    [Theory]
    [InlineData("firstname", false, "c.first_name ASC")]
    [InlineData("lastname", true, "c.last_name DESC")]
    [InlineData("companyname", false, "c.company_name ASC")]
    [InlineData("clienttype", false, "ct.type_name ASC")]
    [InlineData("city", false, "City ASC")]
    [InlineData("primarycontact", true, "PrimaryContact DESC")]
    [InlineData("activepoliciescount", false, "ActivePoliciesCount ASC")]
    [InlineData("invalid", false, "c.client_id ASC")]
    public void GetOrderBy_ShouldMapToCorrectColumn(string sortBy, bool descending, string expected)
    {
        // Act
        var result = GetClientsHandler.GetOrderBy(sortBy, descending);

        // Assert
        result.Should().Be(expected);
    }
}
