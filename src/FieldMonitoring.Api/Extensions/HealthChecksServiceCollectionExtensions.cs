using FieldMonitoring.Api.HealthChecks;
using FieldMonitoring.Infrastructure.HealthChecks;
using FieldMonitoring.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FieldMonitoring.Api.Extensions;

public static class HealthChecksServiceCollectionExtensions
{
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        IHealthChecksBuilder builder = services
            .AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                tags: [HealthCheckTags.Live])
            .AddDbContextCheck<FieldMonitoringDbContext>(
                "sqlserver",
                tags: [HealthCheckTags.Ready],
                customTestQuery: IsDatabaseAvailableAsync);

        builder.AddInfrastructureReadinessChecks(configuration);

        return services;
    }

    private static async Task<bool> IsDatabaseAvailableAsync(
        FieldMonitoringDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.Database.CanConnectAsync(cancellationToken);
    }
}
