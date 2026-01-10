namespace FieldMonitoring.Domain.Alerts;

/// <summary>
/// Representa um alerta com seu ciclo de vida completo (ativo → resolvido).
/// Alertas são gerados quando regras de negócio detectam condições anormais.
/// </summary>
public class Alert
{
    /// <summary>
    /// Identificador único do alerta (Chave Primária).
    /// Gerado automaticamente como GUID.
    /// </summary>
    public Guid AlertId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identificador da fazenda onde o alerta foi gerado.
    /// </summary>
    public required string FarmId { get; set; }

    /// <summary>
    /// Identificador do talhão onde o alerta foi gerado.
    /// </summary>
    public required string FieldId { get; set; }

    /// <summary>
    /// Tipo do alerta (Dryness, PestRisk, etc.).
    /// Determina qual regra de negócio gerou o alerta.
    /// </summary>
    public AlertType AlertType { get; set; }

    /// <summary>
    /// Nível de severidade do alerta (opcional).
    /// Pode ser usado para priorização no dashboard.
    /// </summary>
    public int? Severity { get; set; }

    /// <summary>
    /// Status atual do ciclo de vida do alerta.
    /// Active = em andamento, Resolved = condição normalizada.
    /// </summary>
    public AlertStatus Status { get; set; } = AlertStatus.Active;

    /// <summary>
    /// Razão legível para o alerta.
    /// Exemplo: "Umidade do solo abaixo de 30% por mais de 24 horas"
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Timestamp de quando a condição de alerta iniciou.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp de quando o alerta foi resolvido (null se ainda ativo).
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Timestamp de quando o registro do alerta foi criado.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Marca o alerta como resolvido.
    /// Chamado quando a condição que gerou o alerta volta ao normal.
    /// </summary>
    public void Resolve(DateTime? resolvedAt = null)
    {
        Status = AlertStatus.Resolved;
        ResolvedAt = resolvedAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Cria um novo alerta de seca para um talhão.
    /// </summary>
    public static Alert CreateDrynessAlert(string farmId, string fieldId, string reason)
    {
        return new Alert
        {
            FarmId = farmId,
            FieldId = fieldId,
            AlertType = AlertType.Dryness,
            Reason = reason,
            Status = AlertStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria um novo alerta de risco de pragas para um talhão.
    /// </summary>
    public static Alert CreatePestRiskAlert(string farmId, string fieldId, string reason)
    {
        return new Alert
        {
            FarmId = farmId,
            FieldId = fieldId,
            AlertType = AlertType.PestRisk,
            Reason = reason,
            Status = AlertStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria um novo alerta de calor extremo para um talhão.
    /// </summary>
    public static Alert CreateExtremeHeatAlert(string farmId, string fieldId, string reason)
    {
        return new Alert
        {
            FarmId = farmId,
            FieldId = fieldId,
            AlertType = AlertType.ExtremeHeat,
            Reason = reason,
            Status = AlertStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria um novo alerta de geada para um talhão.
    /// </summary>
    public static Alert CreateFrostAlert(string farmId, string fieldId, string reason)
    {
        return new Alert
        {
            FarmId = farmId,
            FieldId = fieldId,
            AlertType = AlertType.Frost,
            Reason = reason,
            Status = AlertStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria um novo alerta de ar seco para um talhão.
    /// </summary>
    public static Alert CreateDryAirAlert(string farmId, string fieldId, string reason)
    {
        return new Alert
        {
            FarmId = farmId,
            FieldId = fieldId,
            AlertType = AlertType.DryAir,
            Reason = reason,
            Status = AlertStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria um novo alerta de ar úmido para um talhão.
    /// </summary>
    public static Alert CreateHumidAirAlert(string farmId, string fieldId, string reason)
    {
        return new Alert
        {
            FarmId = farmId,
            FieldId = fieldId,
            AlertType = AlertType.HumidAir,
            Reason = reason,
            Status = AlertStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}
