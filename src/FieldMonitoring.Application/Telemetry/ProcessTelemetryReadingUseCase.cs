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
        try
        {
            // Converte a mensagem SQS para objeto de domínio
            SensorReading reading = message.ToSensorReading();

            // Valida os dados da leitura (campos obrigatórios, ranges válidos)
            if (!reading.IsValid(out var errorMessage))
            {
                _logger.LogWarning("Invalid reading {ReadingId} from field {FieldId}: {Error}", 
                    reading.ReadingId, reading.FieldId, errorMessage);
                return ProcessingResult.Failed($"Invalid reading: {errorMessage}");
            }

            // Verificação de idempotência - evita reprocessamento de leituras duplicadas
            if (await _idempotencyStore.ExistsAsync(reading.ReadingId, cancellationToken))
            {
                return ProcessingResult.Skipped("Reading already processed");
            }

            // Persiste no banco de séries temporais (histórico para consultas)
            await _timeSeriesStore.AppendAsync(reading, cancellationToken);

            // Obtém ou cria o Field aggregate
            Field field = await _fieldRepository.GetByIdAsync(reading.FieldId, cancellationToken)
                ?? Field.Create(reading.FieldId, reading.FarmId);

            Dictionary<Guid, AlertStatus> alertStatusBefore = CaptureAlertStatuses(field);

            // Carrega todas as regras default habilitadas
            List<Rule> rules = new()
            {
                Rule.CreateDefaultDrynessRule(),
                Rule.CreateDefaultExtremeHeatRule(),
                Rule.CreateDefaultFrostRule(),
                Rule.CreateDefaultDryAirRule(),
                Rule.CreateDefaultHumidAirRule()
            };

            // CORE: Processa leitura - toda lógica de negócio está encapsulada no aggregate
            field.ProcessReading(reading, rules);

            // Persiste o aggregate completo (FieldStatus + FieldRuleState + Alerts)
            await _fieldRepository.SaveAsync(field, cancellationToken);

            // Marca a leitura como processada (idempotência)
            await _idempotencyStore.MarkProcessedAsync(
                new ProcessedReading
                {
                    ReadingId = reading.ReadingId,
                    FieldId = reading.FieldId,
                    ProcessedAt = DateTime.UtcNow,
                    Source = reading.Source
                },
                cancellationToken);

            IReadOnlyList<AlertEvent> alertEvents = BuildAlertEvents(alertStatusBefore, field.Alerts);
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

    private static Dictionary<Guid, AlertStatus> CaptureAlertStatuses(Field field)
    {
        return field.Alerts
            .ToDictionary(alert => alert.AlertId, alert => alert.Status);
    }

    private static IReadOnlyList<AlertEvent> BuildAlertEvents(
        IReadOnlyDictionary<Guid, AlertStatus> before,
        IReadOnlyList<Alert> after)
    {
        List<AlertEvent> events = new();

        foreach (Alert alert in after)
        {
            if (!before.TryGetValue(alert.AlertId, out AlertStatus previousStatus))
            {
                events.Add(CreateAlertEvent(alert));
                continue;
            }

            if (previousStatus != alert.Status)
            {
                events.Add(CreateAlertEvent(alert));
            }
        }

        return events;
    }

    private static AlertEvent CreateAlertEvent(Alert alert)
    {
        DateTime occurredAt = alert.Status == AlertStatus.Resolved
            ? alert.ResolvedAt ?? DateTime.Now
            : alert.StartedAt;

        return new AlertEvent
        {
            AlertId = alert.AlertId,
            FarmId = alert.FarmId,
            FieldId = alert.FieldId,
            AlertType = alert.AlertType,
            Status = alert.Status,
            Reason = alert.Reason,
            Severity = alert.Severity,
            OccurredAt = occurredAt
        };
    }

    private async Task PublishAlertEventsAsync(
        IReadOnlyList<AlertEvent> alertEvents,
        CancellationToken cancellationToken)
    {
        if (alertEvents.Count == 0)
        {
            return;
        }

        try
        {
            foreach (AlertEvent alertEvent in alertEvents)
            {
                await _alertEventsStore.AppendAsync(alertEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish {AlertEventCount} alert events.", alertEvents.Count);
        }
    }
}

/// <summary>
/// Resultado do processamento de uma leitura de telemetria.
/// Indica se foi sucesso, se foi pulado (idempotência) ou se falhou.
/// </summary>
public sealed record ProcessingResult
{
    public bool IsSuccess { get; init; }
    public bool WasSkipped { get; init; }
    public string? Message { get; init; }

    public static ProcessingResult Success() => new() { IsSuccess = true };
    public static ProcessingResult Skipped(string reason) => new() { IsSuccess = true, WasSkipped = true, Message = reason };
    public static ProcessingResult Failed(string reason) => new() { IsSuccess = false, Message = reason };
}
