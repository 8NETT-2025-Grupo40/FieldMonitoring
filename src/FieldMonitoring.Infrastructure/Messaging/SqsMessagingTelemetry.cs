using System.Diagnostics;
using FieldMonitoring.Application.Observability;

namespace FieldMonitoring.Infrastructure.Messaging;

/// <summary>
/// Telemetria especifica de mensageria SQS.
/// </summary>
internal static class SqsMessagingTelemetry
{
    public const string SpanSqsConsumeTelemetryMessage = "messaging.sqs.process";

    private const string AttributeMessagingSystem = "messaging.system";
    private const string AttributeMessagingOperation = "messaging.operation";
    private const string AttributeMessagingDestinationName = "messaging.destination.name";
    private const string AttributeMessagingDestinationKind = "messaging.destination.kind";
    private const string AttributeMessagingMessageId = "messaging.message.id";

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

    public static Activity? StartConsumerActivity()
    {
        return FieldMonitoringTelemetry.ActivitySource.StartActivity(
            SpanSqsConsumeTelemetryMessage,
            ActivityKind.Consumer);
    }

    private static string ExtractQueueName(string queueUrl)
    {
        var separatorIndex = queueUrl.LastIndexOf('/');

        return separatorIndex >= 0 && separatorIndex < queueUrl.Length - 1
            ? queueUrl[(separatorIndex + 1)..]
            : queueUrl;
    }
}
