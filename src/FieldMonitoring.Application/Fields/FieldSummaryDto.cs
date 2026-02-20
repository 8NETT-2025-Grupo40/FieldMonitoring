using FieldMonitoring.Domain.Fields;

namespace FieldMonitoring.Application.Fields;

/// <summary>
/// DTO base com propriedades compartilhadas entre visao geral e detalhe de talhao.
/// </summary>
public record FieldSummaryDto
{
    public string FieldId { get; init; } = default!;
    public string FarmId { get; init; } = default!;
    public string? SensorId { get; init; }
    public FieldStatusType Status { get; init; }
    public string StatusName => Status.ToString();
    public string? StatusReason { get; init; }
    public DateTimeOffset? LastReadingAt { get; init; }
    public double? LastSoilHumidity { get; init; }
    public double? LastSoilTemperature { get; init; }
    public double? LastAirTemperature { get; init; }
    public double? LastAirHumidity { get; init; }
    public double? LastRainMm { get; init; }

    /// <summary>
    /// Mapeia as propriedades compartilhadas de um Field de dominio para um DTO derivado.
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
