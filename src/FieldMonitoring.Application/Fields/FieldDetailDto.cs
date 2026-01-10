using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Domain.Fields;

namespace FieldMonitoring.Application.Fields;

/// <summary>
/// DTO para informações detalhadas de um talhão.
/// </summary>
public sealed record FieldDetailDto
{
    /// <summary>
    /// Identificador do talhão.
    /// </summary>
    public required string FieldId { get; init; }

    /// <summary>
    /// Identificador da fazenda.
    /// </summary>
    public required string FarmId { get; init; }

    /// <summary>
    /// Identificador do sensor.
    /// </summary>
    public string? SensorId { get; init; }

    /// <summary>
    /// Status atual do talhão.
    /// </summary>
    public required FieldStatusType Status { get; init; }

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
    public DateTime? LastReadingAt { get; init; }

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
    /// Alertas ativos para este talhão.
    /// </summary>
    public required IReadOnlyList<AlertDto> ActiveAlerts { get; init; }

    /// <summary>
    /// Timestamp de quando o status foi atualizado pela última vez.
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}
