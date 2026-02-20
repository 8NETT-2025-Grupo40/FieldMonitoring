using FieldMonitoring.Api.Tests;
using FieldMonitoring.Infrastructure.Persistence.TimeSeries;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FieldMonitoring.Api.Tests.Controllers;

internal static class HealthControllerTestClientFactory
{
    private const string InfluxReadinessCheckName = "influxdb";
    private const string SqsReadinessCheckName = "sqs";
    private const string ReadinessTag = "ready";
    private static readonly Type InfluxReadinessHealthCheckType =
        typeof(Program).Assembly.GetType(
            "FieldMonitoring.Api.HealthChecks.InfluxDbReadinessHealthCheck",
            throwOnError: true)!;

    public static HttpClient CreateClientWithHealthCheck(
        TestWebApplicationFactory factory,
        string name,
        Func<HealthCheckResult> check,
        string tag)
    {
        WebApplicationFactory<Program> customFactory = factory.WithWebHostBuilder(builder =>
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

    public static HttpClient CreateClientWithoutOptionalReadinessChecks(TestWebApplicationFactory factory)
    {
        WebApplicationFactory<Program> customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(RemoveOptionalReadinessChecks);
        });

        return customFactory.CreateClient();
    }

    public static HttpClient CreateClientWithInfluxBucketProbe(
        TestWebApplicationFactory factory,
        IInfluxBucketProbe probe)
    {
        WebApplicationFactory<Program> customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                ReplaceSingleton(services, CreateEnabledInfluxDbOptions());
                ReplaceSingleton<IInfluxBucketProbe>(services, probe);
                ConfigureInfluxReadinessScenario(services);
            });
        });

        return customFactory.CreateClient();
    }

    private static void ConfigureInfluxReadinessScenario(IServiceCollection services)
    {
        services.PostConfigure<HealthCheckServiceOptions>(options =>
        {
            RemoveOptionalReadinessChecks(options);
            AddInfluxReadinessCheck(options);
        });
    }

    private static void AddInfluxReadinessCheck(HealthCheckServiceOptions options)
    {
        options.Registrations.Add(
            new HealthCheckRegistration(
                InfluxReadinessCheckName,
                CreateInfluxReadinessHealthCheck,
                null,
                [ReadinessTag]));
    }

    private static IHealthCheck CreateInfluxReadinessHealthCheck(IServiceProvider serviceProvider)
    {
        return (IHealthCheck)ActivatorUtilities.CreateInstance(
            serviceProvider,
            InfluxReadinessHealthCheckType);
    }

    private static InfluxDbOptions CreateEnabledInfluxDbOptions()
    {
        return new InfluxDbOptions
        {
            Enabled = true,
            Url = "http://localhost:8086",
            Token = "test-token",
            Org = "test-org",
            Bucket = "test-bucket",
            Measurement = "telemetry_readings",
            AlertMeasurement = "field_alerts"
        };
    }

    private static void ReplaceSingleton<TService>(IServiceCollection services, TService implementation)
        where TService : class
    {
        List<ServiceDescriptor> descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(TService))
            .ToList();

        foreach (ServiceDescriptor descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton(implementation);
    }

    private static void RemoveOptionalReadinessChecks(IServiceCollection services)
    {
        services.PostConfigure<HealthCheckServiceOptions>(RemoveOptionalReadinessChecks);
    }

    private static void RemoveOptionalReadinessChecks(HealthCheckServiceOptions options)
    {
        List<HealthCheckRegistration> optionalRegistrations = options.Registrations
            .Where(registration =>
                string.Equals(registration.Name, InfluxReadinessCheckName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(registration.Name, SqsReadinessCheckName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (HealthCheckRegistration registration in optionalRegistrations)
        {
            options.Registrations.Remove(registration);
        }
    }
}

internal static class InfluxBucketProbeStubs
{
    public static IInfluxBucketProbe Successful()
        => new DelegateInfluxBucketProbe(_ => Task.FromResult(true));

    public static IInfluxBucketProbe Missing()
        => new DelegateInfluxBucketProbe(_ => Task.FromResult(false));

    public static IInfluxBucketProbe Throwing()
        => new DelegateInfluxBucketProbe(_ => throw new InvalidOperationException("forced"));

    private sealed class DelegateInfluxBucketProbe : IInfluxBucketProbe
    {
        private readonly Func<CancellationToken, Task<bool>> _canAccessConfiguredBucket;

        public DelegateInfluxBucketProbe(Func<CancellationToken, Task<bool>> canAccessConfiguredBucket)
        {
            _canAccessConfiguredBucket = canAccessConfiguredBucket;
        }

        public Task<bool> CanAccessConfiguredBucketAsync(CancellationToken cancellationToken = default)
        {
            return _canAccessConfiguredBucket(cancellationToken);
        }
    }
}
