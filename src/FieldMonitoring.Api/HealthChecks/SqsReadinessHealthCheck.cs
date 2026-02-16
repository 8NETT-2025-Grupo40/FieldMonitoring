using Amazon.SQS;
using Amazon.SQS.Model;
using FieldMonitoring.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FieldMonitoring.Api.HealthChecks;

internal sealed class SqsReadinessHealthCheck : IHealthCheck
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(20);
    private readonly IAmazonSQS _sqsClient;
    private readonly IOptions<SqsOptions> _options;

    public SqsReadinessHealthCheck(
        IAmazonSQS sqsClient,
        IOptions<SqsOptions> options)
    {
        _sqsClient = sqsClient;
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        SqsOptions options = _options.Value;

        if (!options.Enabled)
        {
            return HealthCheckResult.Healthy("SQS desabilitado.");
        }

        if (string.IsNullOrWhiteSpace(options.QueueUrl))
        {
            return HealthCheckResult.Unhealthy("A URL da fila SQS não está configurada.");
        }

        if (!Uri.TryCreate(options.QueueUrl, UriKind.Absolute, out _))
        {
            return HealthCheckResult.Unhealthy("A URL da fila SQS é inválida.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ProbeTimeout);

        var request = new GetQueueAttributesRequest
        {
            QueueUrl = options.QueueUrl,
            AttributeNames = new List<string> { "QueueArn" }
        };

        try
        {
            GetQueueAttributesResponse response = await _sqsClient.GetQueueAttributesAsync(request, timeoutCts.Token);

            return response.HttpStatusCode is System.Net.HttpStatusCode.OK
                ? HealthCheckResult.Healthy("SQS acessível.")
                : HealthCheckResult.Unhealthy($"Falha ao consultar fila SQS. HTTP {(int)response.HttpStatusCode}.");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                $"Timeout ao verificar conectividade com SQS ({ProbeTimeout.TotalSeconds:0}s).",
                ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha de conectividade com SQS.", ex);
        }
    }
}
