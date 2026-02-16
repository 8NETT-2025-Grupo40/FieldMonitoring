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
    private readonly ILogger<ProcessTelemetryReadingUseCase> _logger;

    public ProcessTelemetryReadingUseCase(
        IIdempotencyStore idempotencyStore,
        ITimeSeriesReadingsStore timeSeriesStore,
        IAlertEventsStore alertEventsStore,
        IFieldRepository fieldRepository,
        ILogger<ProcessTelemetryReadingUseCase> logger)
    {
        _idempotencyStore = idempotencyStore;
        _timeSeriesStore = timeSeriesStore;
        _alertEventsStore = alertEventsStore;
        _fieldRepository = fieldRepository;
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

            List<Rule> rules = new()
            {
                Rule.CreateDefaultDrynessRule(),
                Rule.CreateDefaultExtremeHeatRule(),
                Rule.CreateDefaultFrostRule(),
                Rule.CreateDefaultDryAirRule(),
                Rule.CreateDefaultHumidAirRule()
            };

            field.ProcessReading(reading, rules);

            await _fieldRepository.SaveAsync(field, cancellationToken);

            await _idempotencyStore.MarkProcessedAsync(
                new ProcessedReading
                {
                    ReadingId = reading.ReadingId,
                    FieldId = reading.FieldId,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    Source = reading.Source
                },
                cancellationToken);

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

    /// <summary>
    /// Insere uma leitura simulada no InfluxDB para testar conexão.
    /// Não executa idempotência, regras ou persistência em SQL.
    /// </summary>
    public async Task<ProcessingResult> InsertMockReadingAsync(
        CancellationToken cancellationToken = default)
    {
        using Activity? activity = FieldMonitoringTelemetry.StartActivity(
            FieldMonitoringTelemetry.SpanInsertMockTelemetryReading,
            ActivityKind.Internal);

        try
        {
            string readingId = $"mock-{Guid.NewGuid():N}";
            Result<SensorReading> readingResult = SensorReading.Create(
                readingId: readingId,
                sensorId: "sensor-mock-001",
                fieldId: "field-mock-001",
                farmId: "farm-mock-001",
                timestamp: DateTimeOffset.UtcNow,
                soilMoisturePercent: 42.5,
                soilTemperatureC: 23.4,
                rainMm: 0.2,
                airTemperatureC: 26.1,
                airHumidityPercent: 55.0,
                source: ReadingSource.Http);

            if (!readingResult.IsSuccess)
            {
                _logger.LogWarning("Falha ao criar leitura simulada: {Error}", readingResult.Error);
                FieldMonitoringTelemetry.MarkFailure(activity, "invalid-mock-reading");
                return ProcessingResult.NonRetryableFailure($"Leitura simulada inválida: {readingResult.Error}");
            }

            await _timeSeriesStore.AppendAsync(readingResult.Value!, cancellationToken);
            FieldMonitoringTelemetry.MarkSuccess(activity);
            return ProcessingResult.Success($"Leitura simulada inserida: {readingId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao inserir leitura simulada no InfluxDB.");
            FieldMonitoringTelemetry.MarkFailure(activity, ex.Message);
            FieldMonitoringTelemetry.RecordException(activity, ex);
            return ProcessingResult.NonRetryableFailure("Falha ao inserir leitura simulada no InfluxDB.");
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
