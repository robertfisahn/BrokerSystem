using System.Net;
using BrokerSystem.Tests.Integration.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BrokerSystem.Tests.Integration.Features.Policies;

public class ExportPolicyTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Export_ExistingPolicy_ReturnsPdfFile()
    {
        // Act
        var response = await _client.GetAsync("/api/policies/1/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
        
        // Basic PDF header check (%PDF-)
        bytes[0].Should().Be(0x25); // %
        bytes[1].Should().Be(0x50); // P
        bytes[2].Should().Be(0x44); // D
        bytes[3].Should().Be(0x46); // F
    }

    [Fact]
    public async Task Export_NonExistingPolicy_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/policies/999/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
