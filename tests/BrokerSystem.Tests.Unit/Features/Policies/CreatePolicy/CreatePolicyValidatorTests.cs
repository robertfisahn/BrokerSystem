using BrokerSystem.Api.Features.Policies.CreatePolicy;
using FluentValidation.TestHelper;
using Xunit;

namespace BrokerSystem.Tests.Unit.Features.Policies.CreatePolicy;

public class CreatePolicyValidatorTests
{
    private readonly CreatePolicyValidator _validator;

    public CreatePolicyValidatorTests()
    {
        _validator = new CreatePolicyValidator();
    }

    [Fact]
    public void Validate_WhenAllFieldsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CreatePolicyCommand(
            PolicyNumber: "POL-12345",
            ClientId: 1,
            PolicyTypeId: 1,
            AgentId: 1,
            PremiumAmount: 500.00m,
            SumInsured: 10000.00m,
            StartDate: DateTime.Today,
            EndDate: DateTime.Today.AddYears(1),
            PaymentFrequency: "Monthly"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("A very long policy number that exceeds fifty characters limit of the database schema")]
    public void Validate_WhenPolicyNumberInvalid_ShouldHaveValidationError(string? policyNumber)
    {
        // Arrange
        var command = CreateValidCommand() with { PolicyNumber = policyNumber! };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PolicyNumber);
    }

    [Fact]
    public void Validate_WhenEndDateBeforeStartDate_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with 
        { 
            StartDate = DateTime.Today, 
            EndDate = DateTime.Today.AddDays(-1) 
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_WhenAmountsAreNonPositive_ShouldHaveValidationError(decimal amount)
    {
        // Arrange
        var command = CreateValidCommand() with 
        { 
            PremiumAmount = amount, 
            SumInsured = amount 
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PremiumAmount);
        result.ShouldHaveValidationErrorFor(x => x.SumInsured);
    }

    private CreatePolicyCommand CreateValidCommand()
    {
        return new CreatePolicyCommand(
            PolicyNumber: "POL-VALID",
            ClientId: 1,
            PolicyTypeId: 1,
            AgentId: 1,
            PremiumAmount: 100m,
            SumInsured: 1000m,
            StartDate: DateTime.Today,
            EndDate: DateTime.Today.AddDays(30),
            PaymentFrequency: "Annual"
        );
    }
}
