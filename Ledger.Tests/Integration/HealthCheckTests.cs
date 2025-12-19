using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;
using Ledger.API;

namespace Ledger.Tests.Integration;

public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthCheckTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", content);
    }

    [Fact]
    public async Task ReadinessCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/readiness");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ready", content);
    }
}

