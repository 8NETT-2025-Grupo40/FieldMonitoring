using FieldMonitoring.Domain.Fields;

namespace FieldMonitoring.Application.Fields;

/// <summary>
/// DTO base com propriedades compartilhadas entre visão geral e detalhe de talhão.
/// </summary>
public record FieldSummaryDto
{
    /// <summary>
    /// Identificador do talhão.
    /// </summary>
    public string FieldId { get; init; } = default!;

    /// <summary>
    /// Identificador da fazenda.
    /// </summary>
    public string FarmId { get; init; } = default!;

    /// <summary>
    /// Identificador do sensor.
    /// </summary>
    public string? SensorId { get; init; }

    /// <summary>
    /// Status atual do talhão.
    /// </summary>
    public FieldStatusType Status { get; init; }

    /// <summary>
    /// Nome legível do status.
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Explicação para o status atual.
    /// </summary>
    public string? StatusReason { get; init; }

    /// <summary>
    /// Timestamp da última leitura do sensor.
    /// </summary>
    public DateTimeOffset? LastReadingAt { get; init; }

    /// <summary>
    /// Último percentual de umidade do solo registrado.
    /// </summary>
    public double? LastSoilHumidity { get; init; }

    /// <summary>
    /// Última temperatura do solo em Celsius registrada.
    /// </summary>
    public double? LastSoilTemperature { get; init; }

    /// <summary>
    /// Última temperatura do ar em Celsius registrada.
    /// </summary>
    public double? LastAirTemperature { get; init; }

    /// <summary>
    /// Último percentual de umidade do ar registrado.
    /// </summary>
    public double? LastAirHumidity { get; init; }

    /// <summary>
    /// Última precipitação em milímetros registrada.
    /// </summary>
    public double? LastRainMm { get; init; }

    /// <summary>
    /// Mapeia as propriedades compartilhadas de um Field de domínio para um DTO derivado.
    /// </summary>
    internal static TDto FromField<TDto>(Field field) where TDto : FieldSummaryDto, new()
    {
        return new TDto
        {
            FieldId = field.FieldId,
            FarmId = field.FarmId,
            SensorId = field.SensorId,
            Status = field.Status,
            StatusReason = field.StatusReason,
            LastReadingAt = field.LastReadingAt,
            LastSoilHumidity = field.LastSoilMoisture?.Percent,
            LastSoilTemperature = field.LastSoilTemperature?.Celsius,
            LastAirTemperature = field.LastAirTemperature?.Celsius,
            LastAirHumidity = field.LastAirHumidity?.Percent,
            LastRainMm = field.LastRain?.Millimeters
        };
    }
}
