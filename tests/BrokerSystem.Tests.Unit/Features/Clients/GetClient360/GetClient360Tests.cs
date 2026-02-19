using BrokerSystem.Api.Features.Clients.GetClient360;
using FluentAssertions;
using Xunit;

namespace BrokerSystem.Tests.Unit.Features.Clients.GetClient360;

public class GetClient360Tests
{
    [Fact]
    public void MapToClient360Dto_WhenDataComplete_ShouldMapEverythingCorrectly()
    {
        // Arrange
        var client = new Client360Dto 
        { 
            ClientId = 1, 
            FirstName = "Jan", 
            LastName = "Kowalski",
            CompanyName = "Jan-Pol",
            TaxId = "123456",
            RegistrationDate = new DateOnly(2020, 1, 1)
        };
        var contacts = new List<Client360ContactDto> { new() { ContactType = "Email", ContactValue = "jan@test.com", IsPrimary = true } };
        var addresses = new List<Client360AddressDto> { new() { City = "Warszawa", IsCurrent = true } };
        var policies = new List<GetClient360Handler.Client360PolicyDtoInternal> 
        { 
            new() { PolicyId = 10, PolicyNumber = "POL/001", Status = "Active" } 
        };
        var claims = new List<GetClient360Handler.Client360ClaimDtoInternal> 
        { 
            new() { ClaimId = 100, ClaimNumber = "CLM/001", PolicyId = 10 } 
        };

        // Act
        var result = GetClient360Handler.MapToClient360Dto(client, contacts, addresses, policies, claims);

        // Assert
        result.ClientId.Should().Be(1);
        result.FirstName.Should().Be("Jan");
        result.CompanyName.Should().Be("Jan-Pol");
        result.TaxId.Should().Be("123456");
        result.RegistrationDate.Should().Be(new DateOnly(2020, 1, 1));
        result.Contacts.Should().HaveCount(1);
        result.Policies.Should().HaveCount(1);
        result.Policies[0].Claims.Should().HaveCount(1);
        result.Policies[0].Claims[0].ClaimNumber.Should().Be("CLM/001");
    }

    [Fact]
    public void MapToClient360Dto_WhenListsAreEmpty_ShouldHandleGracefully()
    {
        // Arrange
        var client = new Client360Dto { ClientId = 1 };
        var contacts = Enumerable.Empty<Client360ContactDto>();
        var addresses = Enumerable.Empty<Client360AddressDto>();
        var policies = Enumerable.Empty<GetClient360Handler.Client360PolicyDtoInternal>();
        var claims = Enumerable.Empty<GetClient360Handler.Client360ClaimDtoInternal>();

        // Act
        var result = GetClient360Handler.MapToClient360Dto(client, contacts, addresses, policies, claims);

        // Assert
        result.Contacts.Should().BeEmpty();
        result.Addresses.Should().BeEmpty();
        result.Policies.Should().BeEmpty();
    }

    [Fact]
    public void MapToClient360Dto_ShouldGroupClaimsByCorrectPolicy()
    {
        // Arrange
        var client = new Client360Dto { ClientId = 1 };
        var policies = new List<GetClient360Handler.Client360PolicyDtoInternal> 
        { 
            new() { PolicyId = 1, PolicyNumber = "P1" },
            new() { PolicyId = 2, PolicyNumber = "P2" }
        };
        var claims = new List<GetClient360Handler.Client360ClaimDtoInternal> 
        { 
            new() { ClaimId = 101, PolicyId = 1, ClaimNumber = "C1-P1" },
            new() { ClaimId = 102, PolicyId = 1, ClaimNumber = "C2-P1" },
            new() { ClaimId = 201, PolicyId = 2, ClaimNumber = "C1-P2" }
        };

        // Act
        var result = GetClient360Handler.MapToClient360Dto(client, [], [], policies, claims);

        // Assert
        var p1 = result.Policies.First(x => x.PolicyId == 1);
        var p2 = result.Policies.First(x => x.PolicyId == 2);

        p1.Claims.Should().HaveCount(2);
        p2.Claims.Should().HaveCount(1);
    }

    [Fact]
    public void MapToClient360Dto_WhenClaimHasOrphanedPolicyId_ShouldBeIgnored()
    {
        // Arrange
        var client = new Client360Dto { ClientId = 1 };
        var policies = new List<GetClient360Handler.Client360PolicyDtoInternal> { new() { PolicyId = 1 } };
        var claims = new List<GetClient360Handler.Client360ClaimDtoInternal> 
        { 
            new() { ClaimId = 999, PolicyId = 999 } 
        };

        // Act
        var result = GetClient360Handler.MapToClient360Dto(client, [], [], policies, claims);

        // Assert
        result.Policies[0].Claims.Should().BeEmpty();
    }

    [Fact]
    public void MapToClient360Dto_WhenNamesAreNull_ShouldStillMapClient()
    {
        // Arrange
        var client = new Client360Dto { ClientId = 99, FirstName = null, LastName = null, CompanyName = null };

        // Act
        var result = GetClient360Handler.MapToClient360Dto(client, [], [], [], []);

        // Assert
        result.ClientId.Should().Be(99);
        result.FirstName.Should().BeNull();
    }

    [Fact]
    public void MapToClient360Dto_ShouldSortContactsAndAddressesByPriorityFlags()
    {
        // Arrange
        var client = new Client360Dto { ClientId = 1 };
        var contacts = new List<Client360ContactDto>
        {
            new() { ContactValue = "Secondary", IsPrimary = false },
            new() { ContactValue = "Primary", IsPrimary = true }
        };
        var addresses = new List<Client360AddressDto>
        {
            new() { City = "Old", IsCurrent = false },
            new() { City = "Current", IsCurrent = true }
        };

        // Act
        var result = GetClient360Handler.MapToClient360Dto(client, contacts, addresses, [], []);

        // Assert
        result.Contacts[0].IsPrimary.Should().BeTrue();
        result.Contacts[0].ContactValue.Should().Be("Primary");
        result.Addresses[0].IsCurrent.Should().BeTrue();
        result.Addresses[0].City.Should().Be("Current");
    }

    [Fact]
    public void GetMainSql_ShouldContainAllRequiredQueries()
    {
        // Arrange
        var sql = GetClient360Handler.GetMainSql;

        // Assert
        sql.Should().Contain("FROM clients");
        sql.Should().Contain("FROM client_contacts");
        sql.Should().Contain("FROM client_addresses");
        sql.Should().Contain("FROM policies");
        sql.Should().Contain("FROM claims");
        sql.Should().Contain("@Id");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_WithInvalidId_ShouldHaveError(int clientId)
    {
        // Arrange
        var validator = new GetClient360Validator();
        var query = new GetClient360Query(clientId);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "ClientId");
    }
}
