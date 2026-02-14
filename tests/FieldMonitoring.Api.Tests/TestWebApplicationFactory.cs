using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Infrastructure.Persistence;
using FieldMonitoring.Infrastructure.Persistence.TimeSeries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Mantemos as fontes padrão do host de teste e sobrescrevemos
            // apenas as chaves necessárias para os cenários de integração.
            Dictionary<string, string?> settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = string.Empty,
                ["COGNITO_REGION"] = "sa-east-1",
                ["COGNITO_USER_POOL_ID"] = "sa-east-1_test",
                ["COGNITO_CLIENT_ID"] = TestAuthHandler.DefaultClientId,
                ["InfluxDb:Enabled"] = "false",
                ["INFLUXDB_ENABLED"] = "false",
                ["INFLUXDB_URL"] = string.Empty,
                ["INFLUXDB_TOKEN"] = string.Empty,
                ["INFLUXDB_ORG"] = string.Empty,
                ["INFLUXDB_BUCKET"] = string.Empty,
                ["Sqs:Enabled"] = "false",
                ["Sqs:QueueUrl"] = string.Empty
            };

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureTestServices(services =>
        {
            List<ServiceDescriptor> descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<FieldMonitoringDbContext>))
                .ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<FieldMonitoringDbContext>(options =>
            {
                options.UseInMemoryDatabase("FieldMonitoringTestDb");
            });

            List<ServiceDescriptor> timeSeriesDescriptors = services
                .Where(d => d.ServiceType == typeof(ITimeSeriesReadingsStore))
                .ToList();

            foreach (var descriptor in timeSeriesDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<ITimeSeriesReadingsStore, InMemoryTimeSeriesAdapter>();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }
}
