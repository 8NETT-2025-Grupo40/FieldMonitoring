using System.Diagnostics;
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Observability;
using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Fields;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;
using Microsoft.Extensions.Logging;

namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// Orquestra o processamento de uma leitura de telemetria.
/// Este é o ponto de entrada principal do Worker quando uma mensagem chega da fila SQS.
/// </summary>
public class ProcessTelemetryReadingUseCase
{
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ITimeSeriesReadingsStore _timeSeriesStore;
    private readonly IAlertEventsStore _alertEventsStore;
    private readonly IFieldRepository _fieldRepository;
    private readonly IRuleSetProvider _ruleSetProvider;
    private readonly ILogger<ProcessTelemetryReadingUseCase> _logger;

    public ProcessTelemetryReadingUseCase(
        IIdempotencyStore idempotencyStore,
        ITimeSeriesReadingsStore timeSeriesStore,
        IAlertEventsStore alertEventsStore,
        IFieldRepository fieldRepository,
        IRuleSetProvider ruleSetProvider,
        ILogger<ProcessTelemetryReadingUseCase> logger)
    {
        _idempotencyStore = idempotencyStore;
        _timeSeriesStore = timeSeriesStore;
        _alertEventsStore = alertEventsStore;
        _fieldRepository = fieldRepository;
        _ruleSetProvider = ruleSetProvider;
        _logger = logger;
    }

    /// <summary>
    /// Processa uma mensagem de leitura de telemetria recebida da fila SQS.
    /// </summary>
    public async Task<ProcessingResult> ExecuteAsync(
        TelemetryReceivedMessage message,
        CancellationToken cancellationToken = default)
    {
        using Activity? activity = FieldMonitoringTelemetry.StartActivity(
            FieldMonitoringTelemetry.SpanProcessTelemetryReading,
            ActivityKind.Internal);

        FieldMonitoringTelemetry.SetReadingContext(activity, message.FieldId, message.FarmId, message.Source);

        try
        {
            SensorReading reading;
            try
            {
                reading = message.ToSensorReading();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "Payload inválido para a leitura {ReadingId} do talhão {FieldId}.",
                    message.ReadingId,
                    message.FieldId);

                FieldMonitoringTelemetry.MarkFailure(activity, "invalid-payload");
                return ProcessingResult.NonRetryableFailure($"Leitura inválida: {ex.Message}");
            }

            if (await _idempotencyStore.ExistsAsync(reading.ReadingId, cancellationToken))
            {
                FieldMonitoringTelemetry.MarkSuccess(activity, skipped: true);
                return ProcessingResult.Skipped("Leitura já processada");
            }

            await _timeSeriesStore.AppendAsync(reading, cancellationToken);

            Field field = await _fieldRepository.GetByIdAsync(reading.FieldId, cancellationToken)
                ?? Field.Create(reading.FieldId, reading.FarmId);

            var alertStatusBefore = AlertEventBuilder.CaptureStatuses(field.Alerts);

            IReadOnlyList<Rule> rules = _ruleSetProvider.GetRules();

            ProcessedReading processedReading = new()
            {
                ReadingId = reading.ReadingId,
                FieldId = reading.FieldId,
                ProcessedAt = DateTimeOffset.UtcNow,
                Source = reading.Source
            };

            bool readingApplied;
            try
            {
                readingApplied = field.ProcessReading(reading, rules);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "Leitura {ReadingId} rejeitada por invariante de domínio no talhão {FieldId}.",
                    message.ReadingId,
                    message.FieldId);

                FieldMonitoringTelemetry.MarkFailure(activity, "domain-invariant-violation");
                return ProcessingResult.NonRetryableFailure($"Leitura rejeitada por invariante de domínio: {ex.Message}");
            }

            if (!readingApplied)
            {
                await _idempotencyStore.MarkProcessedAsync(processedReading, cancellationToken);
                FieldMonitoringTelemetry.MarkSuccess(activity, skipped: true);
                return ProcessingResult.Skipped("Leitura fora de ordem temporal: histórico persistido sem alterar estado operacional.");
            }

            await _fieldRepository.SaveAsync(field, cancellationToken);

            await _idempotencyStore.MarkProcessedAsync(processedReading, cancellationToken);

            var alertEvents = AlertEventBuilder.BuildEvents(alertStatusBefore, field.Alerts);
            await PublishAlertEventsAsync(alertEvents, cancellationToken);

            FieldMonitoringTelemetry.SetAlertEventsCount(activity, alertEvents.Count);
            FieldMonitoringTelemetry.MarkSuccess(activity);

            return ProcessingResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar a leitura {ReadingId} do talhão {FieldId}.",
                message.ReadingId, message.FieldId);

            FieldMonitoringTelemetry.MarkFailure(activity, ex.Message);
            FieldMonitoringTelemetry.RecordException(activity, ex);

            return ProcessingResult.RetryableFailure("Falha transitória durante o processamento da leitura.");
        }
    }

    private async Task PublishAlertEventsAsync(
        IReadOnlyList<AlertEvent> alertEvents,
        CancellationToken cancellationToken)
    {
        if (alertEvents.Count == 0)
            return;

        try
        {
            foreach (AlertEvent alertEvent in alertEvents)
            {
                await _alertEventsStore.AppendAsync(alertEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao publicar {AlertEventCount} eventos de alerta.", alertEvents.Count);
        }
    }
}
