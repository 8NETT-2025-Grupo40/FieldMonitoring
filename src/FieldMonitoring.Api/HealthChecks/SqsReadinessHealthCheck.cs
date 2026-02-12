using FieldMonitoring.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FieldMonitoring.Api.HealthChecks;

internal sealed class SqsReadinessHealthCheck : IHealthCheck
{
    private readonly IOptions<SqsOptions> _options;

    public SqsReadinessHealthCheck(IOptions<SqsOptions> options)
    {
        _options = options;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        SqsOptions options = _options.Value;

        if (!options.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("SQS desabilitado."));
        }

        if (string.IsNullOrWhiteSpace(options.QueueUrl))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("A URL da fila SQS não está configurada."));
        }

        if (!Uri.TryCreate(options.QueueUrl, UriKind.Absolute, out _))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("A URL da fila SQS é inválida."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("SQS configurado."));
    }
}
