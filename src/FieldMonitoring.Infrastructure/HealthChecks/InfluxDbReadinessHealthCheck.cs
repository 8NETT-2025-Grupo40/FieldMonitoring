using FieldMonitoring.Infrastructure.Persistence.TimeSeries;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FieldMonitoring.Infrastructure.HealthChecks;

internal sealed class InfluxDbReadinessHealthCheck : IHealthCheck
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);
    private readonly InfluxDbOptions _options;
    private readonly IInfluxBucketProbe _bucketProbe;

    public InfluxDbReadinessHealthCheck(
        InfluxDbOptions options,
        IInfluxBucketProbe bucketProbe)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _bucketProbe = bucketProbe ?? throw new ArgumentNullException(nameof(bucketProbe));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return HealthCheckResult.Healthy("InfluxDB desabilitado.");
        }

        if (!_options.IsConfigured())
        {
            return HealthCheckResult.Unhealthy("InfluxDB habilitado, mas com configuração incompleta.");
        }

        if (!Uri.TryCreate(_options.Url, UriKind.Absolute, out _))
        {
            return HealthCheckResult.Unhealthy("A URL do InfluxDB é inválida.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ProbeTimeout);

        try
        {
            bool canAccessBucket = await _bucketProbe.CanAccessConfiguredBucketAsync(timeoutCts.Token);

            if (!canAccessBucket)
            {
                return HealthCheckResult.Unhealthy("Não foi possível acessar o bucket configurado no InfluxDB.");
            }

            return HealthCheckResult.Healthy("InfluxDB acessível e bucket configurado disponível.");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                $"Timeout ao verificar conectividade com InfluxDB ({ProbeTimeout.TotalSeconds:0}s).",
                ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha de conectividade com InfluxDB.", ex);
        }
    }
}
