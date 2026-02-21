using FieldMonitoring.Infrastructure.Messaging;
using FieldMonitoring.Infrastructure.Persistence.TimeSeries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Infrastructure.HealthChecks;

/// <summary>
/// Métodos de extensão para registrar health checks de infraestrutura.
/// </summary>
public static class InfrastructureHealthChecksExtensions
{
    /// <summary>
    /// Registra health checks de readiness para serviços de infraestrutura (InfluxDB, SQS).
    /// </summary>
    public static IHealthChecksBuilder AddInfrastructureReadinessChecks(
        this IHealthChecksBuilder builder,
        IConfiguration configuration)
    {
        InfluxDbOptions influxOptions = InfluxDbOptions.Load(configuration);
        if (influxOptions.Enabled)
        {
            builder.AddCheck<InfluxDbReadinessHealthCheck>(
                "influxdb",
                tags: ["ready"]);
        }

        SqsOptions sqsOptions = new();
        configuration.GetSection(SqsOptions.SectionName).Bind(sqsOptions);
        if (sqsOptions.Enabled)
        {
            builder.AddCheck<SqsReadinessHealthCheck>(
                "sqs",
                tags: ["ready"]);
        }

        return builder;
    }
}
