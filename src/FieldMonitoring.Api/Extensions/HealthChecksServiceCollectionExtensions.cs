using FieldMonitoring.Api.HealthChecks;
using FieldMonitoring.Infrastructure.Messaging;
using FieldMonitoring.Infrastructure.Persistence;
using FieldMonitoring.Infrastructure.Persistence.TimeSeries;
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

        AddOptionalReadinessChecks(builder, configuration);

        return services;
    }

    private static void AddOptionalReadinessChecks(IHealthChecksBuilder builder, IConfiguration configuration)
    {
        InfluxDbOptions influxOptions = InfluxDbOptions.Load(configuration);
        if (influxOptions.Enabled)
        {
            builder.AddCheck<InfluxDbReadinessHealthCheck>(
                "influxdb",
                tags: [HealthCheckTags.Ready]);
        }

        SqsOptions sqsOptions = new();
        configuration.GetSection(SqsOptions.SectionName).Bind(sqsOptions);
        if (sqsOptions.Enabled)
        {
            builder.AddCheck<SqsReadinessHealthCheck>(
                "sqs",
                tags: [HealthCheckTags.Ready]);
        }
    }

    private static async Task<bool> IsDatabaseAvailableAsync(
        FieldMonitoringDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsInMemory())
        {
            return true;
        }

        return await dbContext.Database.CanConnectAsync(cancellationToken);
    }
}
