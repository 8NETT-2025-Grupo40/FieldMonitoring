using System.Net;
using System.Net.Http.Json;
using FieldMonitoring.Api.Tests;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FieldMonitoring.Api.Tests.Controllers;

public class HealthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public HealthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLiveness_ShouldReturnOk()
    {
        using HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/monitoring/health");

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
        // Este cenário valida apenas a prontidão base da API.
        // Checks opcionais externos são removidos para evitar flutuação no CI.
        using HttpClient client = HealthControllerTestClientFactory.CreateClientWithoutOptionalReadinessChecks(_factory);

        // Act
        HttpResponseMessage response = await client.GetAsync("/monitoring/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HealthResponse? content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Checks.Should().ContainKey("sqlserver");
    }

    [Fact]
    public async Task GetReadiness_WhenInfluxEnabledAndBucketExists_ShouldReturnOk()
    {
        using HttpClient client = HealthControllerTestClientFactory.CreateClientWithInfluxBucketProbe(
            _factory,
            InfluxBucketProbeStubs.Successful());

        // Act
        HttpResponseMessage response = await client.GetAsync("/monitoring/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HealthResponse? content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Checks.Should().ContainKey("influxdb");
    }

    [Fact]
    public async Task GetReadiness_WhenInfluxEnabledAndBucketIsMissing_ShouldReturnServiceUnavailable()
    {
        using HttpClient client = HealthControllerTestClientFactory.CreateClientWithInfluxBucketProbe(
            _factory,
            InfluxBucketProbeStubs.Missing());

        // Act
        HttpResponseMessage response = await client.GetAsync("/monitoring/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        HealthResponse? content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Unhealthy");
        content.Checks.Should().ContainKey("influxdb");
    }

    [Fact]
    public async Task GetReadiness_WhenInfluxEnabledAndProbeThrows_ShouldReturnServiceUnavailable()
    {
        using HttpClient client = HealthControllerTestClientFactory.CreateClientWithInfluxBucketProbe(
            _factory,
            InfluxBucketProbeStubs.Throwing());

        // Act
        HttpResponseMessage response = await client.GetAsync("/monitoring/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        HealthResponse? content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Unhealthy");
        content.Checks.Should().ContainKey("influxdb");
    }

    [Fact]
    public async Task GetLiveness_WhenAnyLiveCheckIsUnhealthy_ShouldReturnServiceUnavailable()
    {
        using HttpClient client = HealthControllerTestClientFactory.CreateClientWithHealthCheck(
            _factory,
            "forced-live-unhealthy",
            () => HealthCheckResult.Unhealthy("forçado"),
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
        using HttpClient client = HealthControllerTestClientFactory.CreateClientWithHealthCheck(
            _factory,
            "forced-ready-degraded",
            () => HealthCheckResult.Degraded("forçado"),
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
        using HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/monitoring/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string swaggerJson = await response.Content.ReadAsStringAsync();
        swaggerJson.Should().Contain("/monitoring/health");
        swaggerJson.Should().Contain("/monitoring/ready");
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
