using FieldMonitoring.Domain.Alerts;

namespace FieldMonitoring.Application.Alerts;

/// <summary>
/// DTO para representação de alertas em respostas da API.
/// </summary>
public sealed record AlertDto
{
    /// <summary>
    /// Identificador do alerta.
    /// </summary>
    public required Guid AlertId { get; init; }

    /// <summary>
    /// Identificador da fazenda.
    /// </summary>
    public required string FarmId { get; init; }

    /// <summary>
    /// Identificador do talhão.
    /// </summary>
    public required string FieldId { get; init; }

    /// <summary>
    /// Tipo do alerta.
    /// </summary>
    public required AlertType AlertType { get; init; }

    /// <summary>
    /// Nome legível do tipo de alerta.
    /// </summary>
    public string AlertTypeName => AlertType.ToString();

    /// <summary>
    /// Nível de severidade (opcional).
    /// </summary>
    public int? Severity { get; init; }

    /// <summary>
    /// Status atual do ciclo de vida.
    /// </summary>
    public required AlertStatus Status { get; init; }

    /// <summary>
    /// Nome legível do status.
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Razão do alerta.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Quando o alerta iniciou.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Quando o alerta foi resolvido (null se ativo).
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; init; }

    /// <summary>
    /// Mapeia um Alert de domínio para AlertDto.
    /// </summary>
    public static AlertDto FromEntity(Alert alert)
    {
        return new AlertDto
        {
            AlertId = alert.AlertId,
            FarmId = alert.FarmId,
            FieldId = alert.FieldId,
            AlertType = alert.AlertType,
            Severity = alert.Severity,
            Status = alert.Status,
            Reason = alert.Reason,
            StartedAt = alert.StartedAt,
            ResolvedAt = alert.ResolvedAt
        };
    }
}
