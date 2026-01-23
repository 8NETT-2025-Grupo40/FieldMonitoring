using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Fields;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;
using Microsoft.Extensions.Logging;

namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// Orquestra o processamento de uma leitura de telemetria.
/// Este e o ponto de entrada principal do Worker quando uma mensagem chega da fila SQS.
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
        try
        {
            SensorReading reading = message.ToSensorReading();

            if (!reading.IsValid(out var errorMessage))
            {
                _logger.LogWarning("Invalid reading {ReadingId} from field {FieldId}: {Error}",
                    reading.ReadingId, reading.FieldId, errorMessage);
                return ProcessingResult.Failed($"Invalid reading: {errorMessage}");
            }

            if (await _idempotencyStore.ExistsAsync(reading.ReadingId, cancellationToken))
            {
                return ProcessingResult.Skipped("Reading already processed");
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

            return ProcessingResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed for reading {ReadingId} in field {FieldId}",
                message.ReadingId, message.FieldId);

            return ProcessingResult.Failed($"Processing error: {ex.Message}");
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
            _logger.LogWarning(ex, "Failed to publish {AlertEventCount} alert events.", alertEvents.Count);
        }
    }
}

/// <summary>
/// Status do processamento de uma leitura.
/// </summary>
public enum ProcessingStatus
{
    Success,
    Skipped,
    Failed
}

/// <summary>
/// Resultado do processamento de uma leitura de telemetria.
/// </summary>
public sealed record ProcessingResult(ProcessingStatus Status, string? Message = null)
{
    public bool IsSuccess => Status is ProcessingStatus.Success or ProcessingStatus.Skipped;
    public bool WasSkipped => Status is ProcessingStatus.Skipped;

    public static ProcessingResult Success() => new(ProcessingStatus.Success);
    public static ProcessingResult Skipped(string reason) => new(ProcessingStatus.Skipped, reason);
    public static ProcessingResult Failed(string reason) => new(ProcessingStatus.Failed, reason);
}
