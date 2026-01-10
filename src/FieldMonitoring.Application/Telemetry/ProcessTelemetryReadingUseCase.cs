using FieldMonitoring.Application.Fields;
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
    private readonly IFieldRepository _fieldRepository;
    private readonly ILogger<ProcessTelemetryReadingUseCase> _logger;

    public ProcessTelemetryReadingUseCase(
        IIdempotencyStore idempotencyStore,
        ITimeSeriesReadingsStore timeSeriesStore,
        IFieldRepository fieldRepository,
        ILogger<ProcessTelemetryReadingUseCase> logger)
    {
        _idempotencyStore = idempotencyStore;
        _timeSeriesStore = timeSeriesStore;
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

            return ProcessingResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed for reading {ReadingId} in field {FieldId}",
                message.ReadingId, message.FieldId);

            return ProcessingResult.Failed($"Processing error: {ex.Message}");
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
