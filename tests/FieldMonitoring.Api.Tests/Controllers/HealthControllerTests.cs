using System.Net;
using System.Net.Http.Json;
using FieldMonitoring.Api.Tests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FieldMonitoring.Api.Tests.Controllers;

public class HealthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLiveness_ShouldReturnOk()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/monitoring/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HealthResponse? content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Checks.Should().ContainKey("self");
    }

    [Fact]
    public async Task GetReadiness_ShouldReturnOk()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/monitoring/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HealthResponse? content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Checks.Should().ContainKey("sqlserver");
    }

    [Fact]
    public async Task GetLiveness_WhenAnyLiveCheckIsUnhealthy_ShouldReturnServiceUnavailable()
    {
        using HttpClient client = CreateClientWithHealthCheck(
            "forced-live-unhealthy",
            () => HealthCheckResult.Unhealthy("forcado"),
            "live");

        // Act
        HttpResponseMessage response = await client.GetAsync("/monitoring/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        HealthResponse? content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Unhealthy");
        content.Checks.Should().ContainKey("forced-live-unhealthy");
    }

    [Fact]
    public async Task GetReadiness_WhenAnyReadyCheckIsDegraded_ShouldReturnServiceUnavailable()
    {
        using HttpClient client = CreateClientWithHealthCheck(
            "forced-ready-degraded",
            () => HealthCheckResult.Degraded("forcado"),
            "ready");

        // Act
        HttpResponseMessage response = await client.GetAsync("/monitoring/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        HealthResponse? content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Degraded");
        content.Checks.Should().ContainKey("forced-ready-degraded");
    }

    [Fact]
    public async Task Swagger_ShouldContainHealthEndpoints()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/monitoring/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string swaggerJson = await response.Content.ReadAsStringAsync();
        swaggerJson.Should().Contain("/monitoring/health");
        swaggerJson.Should().Contain("/monitoring/ready");
    }

    private HttpClient CreateClientWithHealthCheck(
        string name,
        Func<HealthCheckResult> check,
        string tag)
    {
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                services
                    .AddHealthChecks()
                    .AddCheck(name, check, tags: [tag]);
            });
        });

        return customFactory.CreateClient();
    }

    private record HealthResponse(
        string Status,
        DateTimeOffset Timestamp,
        Dictionary<string, HealthCheckEntryResponse> Checks);

    private record HealthCheckEntryResponse(
        string Status,
        string? Description,
        double DurationMs);
}
