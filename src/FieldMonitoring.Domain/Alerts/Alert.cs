namespace FieldMonitoring.Domain.Alerts;

/// <summary>
/// Representa um alerta com seu ciclo de vida completo (ativo -> resolvido).
/// Alertas sao gerados quando regras de negocio detectam condicoes anormais.
/// </summary>
public class Alert
{
    private Alert() { }

    public Guid AlertId { get; private set; } = Guid.NewGuid();
    public string FarmId { get; private set; } = null!;
    public string FieldId { get; private set; } = null!;

    /// <summary>
    /// Tipo do alerta (Dryness, ExtremeHeat, Frost, DryAir, HumidAir).
    /// </summary>
    public AlertType AlertType { get; private set; }

    /// <summary>
    /// Severidade numérica (menor = mais crítico).
    /// </summary>
    public int? Severity { get; private set; }

    /// <summary>
    /// Estado atual do alerta (Ativo ou Resolvido).
    /// </summary>
    public AlertStatus Status { get; private set; } = AlertStatus.Active;

    /// <summary>
    /// Motivo descritivo da condição que gerou o alerta.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Início da condição anormal.
    /// </summary>
    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Quando foi resolvido (null enquanto ativo).
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Marca o alerta como resolvido quando a condicao volta ao normal.
    /// </summary>
    public void Resolve(DateTimeOffset? resolvedAt = null)
    {
        if (Status == AlertStatus.Resolved)
            return;

        Status = AlertStatus.Resolved;
        ResolvedAt = resolvedAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Factory method para criar um novo alerta associado a um talhao.
    /// </summary>
    public static Alert Create(AlertType type, string farmId, string fieldId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(farmId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new Alert
        {
            FarmId = farmId,
            FieldId = fieldId,
            AlertType = type,
            Severity = type.GetSeverity(),
            Reason = reason,
            Status = AlertStatus.Active,
            StartedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

}
