using BrokerSystem.Api.Features.Policies.GetPolicies;
using BrokerSystem.Api.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace BrokerSystem.Tests.Unit.Features.Policies.GetPolicies;

public class GetPoliciesTests
{
    private readonly GetPoliciesValidator _validator = new();

    [Fact]
    public void GetMainSql_ShouldContainRequiredColumnsAndJoins()
    {
        // Arrange
        var sqlDialect = new SqlServerDialect();
        var where = "WHERE 1=1";
        var orderBy = "p.created_at DESC";

        // Act
        var sql = GetPoliciesHandler.GetMainSql(sqlDialect, where, orderBy);

        // Assert
        sql.Should().Contain("p.policy_id AS PolicyId");
        sql.Should().Contain("pt.type_name AS PolicyType");
        sql.Should().Contain("INNER JOIN clients c");
        sql.Should().Contain("INNER JOIN policy_statuses ps");
        sql.Should().Contain("Status");
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Validator_WithInvalidData_ShouldHaveErrors(int page, int pageSize)
    {
        // Arrange
        var query = new GetPoliciesQuery(page, pageSize);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BuildFilterQuery_WhenSearchIsEmpty_ShouldReturnDefaultWhereClause()
    {
        // Act
        var (whereClause, parameters) = GetPoliciesHandler.BuildFilterQuery(null);

        // Assert
        whereClause.Should().Be("WHERE 1=1");
        parameters.ParameterNames.Should().BeEmpty();
    }

    [Theory]
    [InlineData("POL/001", 1, new[] { "%POL/001%" })]
    [InlineData("POL/001 Jan", 2, new[] { "%POL/001%", "%Jan%" })]
    [InlineData("  Kowalski  123  ", 2, new[] { "%Kowalski%", "%123%" })]
    public void BuildFilterQuery_WhenSearchHasValue_ShouldCreateMultipleLikeConditions(string search, int expectedParamCount, string[] expectedParams)
    {
        // Act
        var (whereClause, parameters) = GetPoliciesHandler.BuildFilterQuery(search);

        // Assert
        parameters.ParameterNames.Should().HaveCount(expectedParamCount);
        for (int i = 0; i < expectedParamCount; i++)
        {
            var pName = $"@p{i}";
            whereClause.Should().Contain($"(p.policy_number LIKE {pName} OR c.first_name LIKE {pName} OR c.last_name LIKE {pName})");
            parameters.Get<string>(pName).Should().Be(expectedParams[i]);
        }
    }

    [Theory]
    [InlineData("policynumber", false, "p.policy_number ASC")]
    [InlineData("clientname", true, "c.last_name DESC")]
    [InlineData("totalpremium", false, "p.premium_amount ASC")]
    [InlineData("status", true, "ps.status_name DESC")]
    [InlineData(null, false, "p.created_at ASC")]
    [InlineData("invalid", false, "p.created_at ASC")]
    public void GetOrderBy_ShouldMapToCorrectColumn(string? sortBy, bool descending, string expected)
    {
        // Act
        var result = GetPoliciesHandler.GetOrderBy(sortBy, descending);

        // Assert
        result.Should().Be(expected);
    }
}
