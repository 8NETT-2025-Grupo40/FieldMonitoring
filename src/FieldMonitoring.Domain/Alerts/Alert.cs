namespace FieldMonitoring.Domain.Alerts;

/// <summary>
/// Representa um alerta com seu ciclo de vida completo (ativo → resolvido).
/// Alertas são gerados quando regras de negócio detectam condições anormais.
/// </summary>
public class Alert
{
    /// <summary>
    /// Construtor privado para reidratação pelo EF Core.
    /// </summary>
    private Alert() { }

    /// <summary>
    /// Identificador único do alerta (Chave Primária).
    /// Gerado automaticamente como GUID.
    /// </summary>
    public Guid AlertId { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Identificador da fazenda onde o alerta foi gerado.
    /// </summary>
    public string FarmId { get; private set; } = null!;

    /// <summary>
    /// Identificador do talhão onde o alerta foi gerado.
    /// </summary>
    public string FieldId { get; private set; } = null!;

    /// <summary>
    /// Tipo do alerta (Dryness, ExtremeHeat, etc.).
    /// Determina qual regra de negócio gerou o alerta.
    /// </summary>
    public AlertType AlertType { get; private set; }

    /// <summary>
    /// Nível de severidade do alerta (opcional).
    /// Pode ser usado para priorização no dashboard.
    /// </summary>
    public int? Severity { get; private set; }

    /// <summary>
    /// Status atual do ciclo de vida do alerta.
    /// Active = em andamento, Resolved = condição normalizada.
    /// </summary>
    public AlertStatus Status { get; private set; } = AlertStatus.Active;

    /// <summary>
    /// Razão legível para o alerta.
    /// Exemplo: "Umidade do solo abaixo de 30% por mais de 24 horas"
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Timestamp de quando a condição de alerta iniciou.
    /// </summary>
    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp de quando o alerta foi resolvido (null se ainda ativo).
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>
    /// Timestamp de quando o registro do alerta foi criado.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Marca o alerta como resolvido.
    /// Chamado quando a condição que gerou o alerta volta ao normal.
    /// </summary>
    public void Resolve(DateTimeOffset? resolvedAt = null)
    {
        Status = AlertStatus.Resolved;
        ResolvedAt = resolvedAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cria um novo alerta para um talhão.
    /// Factory unificada que substitui os métodos individuais por tipo.
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
