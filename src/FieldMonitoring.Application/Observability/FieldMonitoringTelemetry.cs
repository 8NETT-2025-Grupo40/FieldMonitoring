using System.Diagnostics;

namespace FieldMonitoring.Application.Observability;

/// <summary>
/// Centraliza nomes de spans/tags e helpers de telemetria para manter consistência.
/// </summary>
public static class FieldMonitoringTelemetry
{
    /// <summary>
    /// ActivitySource usada pelos spans manuais do domínio.
    /// </summary>
    public const string ActivitySourceName = "FieldMonitoring";

    /// <summary>
    /// Nome do span para processamento de telemetria.
    /// </summary>
    public const string SpanProcessTelemetryReading = "telemetry.process";

    /// <summary>
    /// Status de processamento com sucesso.
    /// </summary>
    public const string ProcessingStatusSuccess = "success";

    /// <summary>
    /// Status de processamento ignorado por idempotência.
    /// </summary>
    public const string ProcessingStatusSkipped = "skipped";

    /// <summary>
    /// Status de processamento com falha.
    /// </summary>
    public const string ProcessingStatusFailed = "failed";

    // Atributos de negócio com namespace próprio para evitar colisão.
    private const string AttributeFieldId = "fieldmonitoring.field.id";
    private const string AttributeFarmId = "fieldmonitoring.farm.id";
    private const string AttributeReadingSource = "fieldmonitoring.reading.source";
    private const string AttributeProcessingStatus = "fieldmonitoring.processing.status";
    private const string AttributeProcessingSkipped = "fieldmonitoring.processing.skipped";
    private const string AttributeProcessingAlertEventsCount = "fieldmonitoring.processing.alert_events_count";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>
    /// Cria um span somente quando existe listener ativo.
    /// </summary>
    /// <param name="name">Nome do span.</param>
    /// <param name="kind">Tipo do span.</param>
    /// <returns>Span criado ou null quando não há listener.</returns>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(name, kind);
    }

    /// <summary>
    /// Adiciona contexto básico da leitura com tags não vazias.
    /// </summary>
    /// <param name="activity">Span atual.</param>
    /// <param name="fieldId">Identificador do talhão.</param>
    /// <param name="farmId">Identificador da fazenda.</param>
    /// <param name="source">Origem da leitura.</param>
    public static void SetReadingContext(Activity? activity, string? fieldId, string? farmId, string? source)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(fieldId))
        {
            activity.SetTag(AttributeFieldId, fieldId);
        }

        if (!string.IsNullOrWhiteSpace(farmId))
        {
            activity.SetTag(AttributeFarmId, farmId);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            activity.SetTag(AttributeReadingSource, source);
        }
    }

    /// <summary>
    /// Marca processamento como sucesso e registra se houve skip.
    /// </summary>
    /// <param name="activity">Span atual.</param>
    /// <param name="skipped">Indica se o item foi ignorado.</param>
    public static void MarkSuccess(Activity? activity, bool skipped = false)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(AttributeProcessingStatus, skipped ? ProcessingStatusSkipped : ProcessingStatusSuccess);

        if (skipped)
        {
            activity.SetTag(AttributeProcessingSkipped, true);
        }

        activity.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Registra quantidade de eventos de alerta gerados no processamento.
    /// </summary>
    /// <param name="activity">Span atual.</param>
    /// <param name="count">Quantidade de eventos de alerta.</param>
    public static void SetAlertEventsCount(Activity? activity, int count)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(AttributeProcessingAlertEventsCount, count);
    }

    /// <summary>
    /// Marca processamento como falha com motivo para diagnóstico.
    /// </summary>
    /// <param name="activity">Span atual.</param>
    /// <param name="reason">Motivo da falha.</param>
    public static void MarkFailure(Activity? activity, string reason)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(AttributeProcessingStatus, ProcessingStatusFailed);
        activity.SetStatus(ActivityStatusCode.Error, reason);
    }

    /// <summary>
    /// Registra exceção como evento para facilitar análise no trace.
    /// </summary>
    /// <param name="activity">Span atual.</param>
    /// <param name="exception">Exceção capturada.</param>
    public static void RecordException(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        activity.AddEvent(new ActivityEvent(
            "exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName },
                { "exception.message", exception.Message },
                { "exception.stacktrace", exception.StackTrace }
            }));
    }
}
