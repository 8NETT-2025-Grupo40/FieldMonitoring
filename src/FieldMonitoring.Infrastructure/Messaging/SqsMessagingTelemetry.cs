using System.Diagnostics;
using FieldMonitoring.Application.Observability;

namespace FieldMonitoring.Infrastructure.Messaging;

/// <summary>
/// Telemetria específica de mensageria SQS — mantida em Infrastructure
/// porque é um detalhe de implementação do transporte.
/// </summary>
internal static class SqsMessagingTelemetry
{
    /// <summary>
    /// Nome do span para consumo de mensagem SQS.
    /// </summary>
    public const string SpanSqsConsumeTelemetryMessage = "messaging.sqs.process";

    // Atributos semânticos de mensageria.
    private const string AttributeMessagingSystem = "messaging.system";
    private const string AttributeMessagingOperation = "messaging.operation";
    private const string AttributeMessagingDestinationName = "messaging.destination.name";
    private const string AttributeMessagingDestinationKind = "messaging.destination.kind";
    private const string AttributeMessagingMessageId = "messaging.message.id";

    /// <summary>
    /// Adiciona contexto de SQS com baixa cardinalidade.
    /// </summary>
    /// <param name="activity">Span atual.</param>
    /// <param name="queueUrl">URL da fila SQS.</param>
    /// <param name="messageId">Identificador da mensagem.</param>
    public static void SetSqsContext(Activity? activity, string? queueUrl, string? messageId)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(AttributeMessagingSystem, "aws.sqs");
        activity.SetTag(AttributeMessagingOperation, "process");
        activity.SetTag(AttributeMessagingDestinationKind, "queue");

        if (!string.IsNullOrWhiteSpace(queueUrl))
        {
            activity.SetTag(AttributeMessagingDestinationName, ExtractQueueName(queueUrl));
        }

        if (!string.IsNullOrWhiteSpace(messageId))
        {
            activity.SetTag(AttributeMessagingMessageId, messageId);
        }
    }

    /// <summary>
    /// Cria um span de consumo SQS somente quando existe listener ativo.
    /// </summary>
    public static Activity? StartConsumerActivity()
    {
        return FieldMonitoringTelemetry.ActivitySource.StartActivity(
            SpanSqsConsumeTelemetryMessage,
            ActivityKind.Consumer);
    }

    /// <summary>
    /// Extrai apenas o nome da fila para reduzir cardinalidade de tags.
    /// </summary>
    /// <param name="queueUrl">URL completa da fila.</param>
    /// <returns>Nome da fila.</returns>
    private static string ExtractQueueName(string queueUrl)
    {
        var separatorIndex = queueUrl.LastIndexOf('/');

        return separatorIndex >= 0 && separatorIndex < queueUrl.Length - 1
            ? queueUrl[(separatorIndex + 1)..]
            : queueUrl;
    }
}
