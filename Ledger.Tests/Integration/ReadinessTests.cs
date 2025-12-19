using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;
using Ledger.API;

namespace Ledger.Tests.Integration;

public class ReadinessTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ReadinessTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Readiness_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/readiness");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

