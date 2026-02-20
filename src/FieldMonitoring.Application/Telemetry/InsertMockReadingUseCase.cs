using System.Diagnostics;
using FieldMonitoring.Application.Observability;
using FieldMonitoring.Domain;
using FieldMonitoring.Domain.Telemetry;
using Microsoft.Extensions.Logging;

namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// Insere uma leitura simulada no InfluxDB para testar conexão.
/// Não executa idempotência, regras ou persistência em SQL.
/// </summary>
public class InsertMockReadingUseCase
{
    private readonly ITimeSeriesReadingsStore _timeSeriesStore;
    private readonly ILogger<InsertMockReadingUseCase> _logger;

    public InsertMockReadingUseCase(
        ITimeSeriesReadingsStore timeSeriesStore,
        ILogger<InsertMockReadingUseCase> logger)
    {
        _timeSeriesStore = timeSeriesStore;
        _logger = logger;
    }

    /// <summary>
    /// Insere uma leitura simulada no InfluxDB para testar conexão.
    /// </summary>
    public async Task<ProcessingResult> ExecuteAsync(
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
}
