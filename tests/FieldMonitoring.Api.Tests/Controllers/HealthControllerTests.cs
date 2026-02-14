using System.Net;
using System.Net.Http.Json;
using FieldMonitoring.Api.Tests;
using FieldMonitoring.Infrastructure.Persistence.TimeSeries;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
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
        // Este cenário valida apenas a prontidão base da API.
        // Checks opcionais externos são removidos para evitar flutuação no CI.
        using HttpClient client = CreateClientWithoutOptionalReadinessChecks();

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
        using HttpClient client = CreateClientWithInfluxBucketProbe(new SuccessfulInfluxBucketProbe());

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
        using HttpClient client = CreateClientWithInfluxBucketProbe(new MissingInfluxBucketProbe());

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
        using HttpClient client = CreateClientWithInfluxBucketProbe(new ThrowingInfluxBucketProbe());

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
                RemoveOptionalReadinessChecks(services);

                services
                    .AddHealthChecks()
                    .AddCheck(name, check, tags: [tag]);
            });
        });

        return customFactory.CreateClient();
    }

    private HttpClient CreateClientWithoutOptionalReadinessChecks()
    {
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            // Os checks opcionais (Influx/SQS) têm testes dedicados; aqui isolamos
            // o comportamento padrão de readiness para manter o teste determinístico.
            builder.ConfigureTestServices(RemoveOptionalReadinessChecks);
        });

        return customFactory.CreateClient();
    }

    private HttpClient CreateClientWithInfluxBucketProbe(IInfluxBucketProbe probe)
    {
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                Dictionary<string, string?> settings = new()
                {
                    ["InfluxDb:Enabled"] = "true",
                    ["INFLUXDB_ENABLED"] = "true",
                    ["InfluxDb:Url"] = "http://localhost:8086",
                    ["INFLUXDB_URL"] = "http://localhost:8086",
                    ["InfluxDb:Token"] = "test-token",
                    ["INFLUXDB_TOKEN"] = "test-token",
                    ["InfluxDb:Org"] = "test-org",
                    ["INFLUXDB_ORG"] = "test-org",
                    ["InfluxDb:Bucket"] = "test-bucket",
                    ["INFLUXDB_BUCKET"] = "test-bucket",
                    ["InfluxDb:Measurement"] = "telemetry_readings",
                    ["InfluxDb:AlertMeasurement"] = "field_alerts",
                    ["Sqs:Enabled"] = "false"
                };

                config.AddInMemoryCollection(settings);
            });

            builder.ConfigureTestServices(services =>
            {
                List<ServiceDescriptor> probeDescriptors = services
                    .Where(d => d.ServiceType == typeof(IInfluxBucketProbe))
                    .ToList();

                foreach (var descriptor in probeDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(probe);
            });
        });

        return customFactory.CreateClient();
    }

    private sealed class SuccessfulInfluxBucketProbe : IInfluxBucketProbe
    {
        public Task<bool> CanAccessConfiguredBucketAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class MissingInfluxBucketProbe : IInfluxBucketProbe
    {
        public Task<bool> CanAccessConfiguredBucketAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    private sealed class ThrowingInfluxBucketProbe : IInfluxBucketProbe
    {
        public Task<bool> CanAccessConfiguredBucketAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("forced");
    }

    private static void RemoveOptionalReadinessChecks(IServiceCollection services)
    {
        services.PostConfigure<HealthCheckServiceOptions>(options =>
        {
            // Em CI, variáveis de ambiente herdadas podem habilitar checks opcionais.
            // Remover explicitamente evita que o readiness padrão fique instável.
            List<HealthCheckRegistration> optionalRegistrations = options.Registrations
                .Where(registration =>
                string.Equals(registration.Name, "influxdb", StringComparison.OrdinalIgnoreCase)
                || string.Equals(registration.Name, "sqs", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (HealthCheckRegistration registration in optionalRegistrations)
            {
                options.Registrations.Remove(registration);
            }
        });
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
