using FieldMonitoring.Infrastructure.Persistence.TimeSeries;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FieldMonitoring.Api.HealthChecks;

internal sealed class InfluxDbReadinessHealthCheck : IHealthCheck
{
    private readonly InfluxDbOptions _options;

    public InfluxDbReadinessHealthCheck(InfluxDbOptions options)
    {
        _options = options;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("InfluxDB desabilitado."));
        }

        if (!_options.IsConfigured())
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("InfluxDB habilitado, mas com configuração incompleta."));
        }

        if (!Uri.TryCreate(_options.Url, UriKind.Absolute, out _))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("A URL do InfluxDB é inválida."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("InfluxDB configurado."));
    }
}
