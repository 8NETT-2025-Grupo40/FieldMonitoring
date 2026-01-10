using FieldMonitoring.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace FieldMonitoring.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = string.Empty,
                ["COGNITO_REGION"] = "sa-east-1",
                ["COGNITO_USER_POOL_ID"] = "sa-east-1_test",
                ["COGNITO_CLIENT_ID"] = TestAuthHandler.DefaultClientId
            };

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureTestServices(services =>
        {
            var descriptors = services
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
