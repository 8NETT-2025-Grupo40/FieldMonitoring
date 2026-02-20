using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Fields.RuleEvaluation;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields;

/// <summary>
/// Aggregate Root que representa um Talhão (Field).
/// Encapsula seu status, alertas e lógica de processamento de leituras.
/// Garante invariantes: só 1 alerta ativo por tipo, transições de status válidas.
/// </summary>
public class Field
{
    // Identificação
    public string FieldId { get; private set; }
    public string FarmId { get; private set; }
    public string? SensorId { get; private set; }

    // Status atual do talhão
    public FieldStatusType Status { get; private set; } = FieldStatusType.Normal;
    public string? StatusReason { get; private set; }
    public DateTimeOffset? LastReadingAt { get; private set; }
    public SoilMoisture? LastSoilMoisture { get; private set; }
    public Temperature? LastSoilTemperature { get; private set; }
    public Temperature? LastAirTemperature { get; private set; }
    public AirHumidity? LastAirHumidity { get; private set; }
    public RainMeasurement? LastRain { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // Estado das regras - propriedades persistidas pelo EF Core
    // Mantidas para compatibilidade com banco existente
    public DateTimeOffset? LastTimeAboveDryThreshold { get; private set; }
    public DateTimeOffset? LastTimeBelowHeatThreshold { get; private set; }
    public DateTimeOffset? LastTimeAboveFrostThreshold { get; private set; }
    public DateTimeOffset? LastTimeAboveDryAirThreshold { get; private set; }
    public DateTimeOffset? LastTimeBelowHumidAirThreshold { get; private set; }

    // Dicionário interno de alertas ativos (in-memory)
    private readonly Dictionary<AlertType, bool> _activeAlerts = new();

    // Avaliadores de regras (Strategy Pattern) - compartilhados entre instâncias
    private static readonly Dictionary<RuleType, IRuleEvaluator> Evaluators = new()
    {
        [RuleType.Dryness] = new DrynessRuleEvaluator(),
        [RuleType.ExtremeHeat] = new ExtremeHeatRuleEvaluator(),
        [RuleType.Frost] = new FrostRuleEvaluator(),
        [RuleType.DryAir] = new DryAirRuleEvaluator(),
        [RuleType.HumidAir] = new HumidAirRuleEvaluator()
    };

    // Coleção de alertas gerenciada pelo aggregate
    private List<Alert> _alerts = new();
    public IReadOnlyList<Alert> Alerts => _alerts.AsReadOnly();

    // Construtor privado - força uso de factory methods
    private Field(string fieldId, string farmId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);
        ArgumentException.ThrowIfNullOrWhiteSpace(farmId);

        FieldId = fieldId;
        FarmId = farmId;
    }

    /// <summary>
    /// Cria um novo talhão.
    /// </summary>
    public static Field Create(string fieldId, string farmId)
    {
        return new Field(fieldId, farmId);
    }

    /// <summary>
    /// Reidrata estado derivado do aggregate após carregamento pelo repositório.
    /// </summary>
    public void Rehydrate()
    {
        RebuildAlertState();
    }

    private void RebuildAlertState()
    {
        _activeAlerts.Clear();

        foreach (var alert in _alerts.Where(a => a.Status == AlertStatus.Active))
        {
            _activeAlerts[alert.AlertType] = true;
        }
    }

    /// <summary>
    /// Reidrata alertas no aggregate e recalcula estado derivado.
    /// Útil em cenários sem Include automático do EF.
    /// </summary>
    public void RehydrateAlerts(IEnumerable<Alert> alerts)
    {
        ArgumentNullException.ThrowIfNull(alerts);

        _alerts.Clear();
        _alerts.AddRange(alerts);
        Rehydrate();
    }

    /// <summary>
    /// Processa uma nova leitura de sensor.
    /// Atualiza estado, avalia regras e gerencia alertas.
    /// Retorna false quando a leitura está fora de ordem temporal e foi ignorada para estado operacional.
    /// </summary>
    public bool ProcessReading(SensorReading reading, IReadOnlyList<Rule> rules)
    {
        if (reading.FieldId != FieldId)
            throw new InvalidOperationException($"Reading pertence a outro talhão: {reading.FieldId}");

        if (reading.FarmId != FarmId)
            throw new InvalidOperationException($"Reading pertence a outra fazenda: {reading.FarmId}");

        if (LastReadingAt.HasValue && reading.Timestamp < LastReadingAt.Value)
            return false;

        UpdateLastReadingValues(reading);

        var context = CreateEvaluationContext();

        foreach (var rule in rules.Where(r => r.IsEnabled))
        {
            if (Evaluators.TryGetValue(rule.RuleType, out var evaluator))
            {
                var result = evaluator.Evaluate(reading, rule, context);
                ApplyEvaluationResult(result, evaluator.AlertType);
            }
        }

        ApplyContextToProperties(context);
        UpdateStatus();

        return true;
    }

    /// <summary>
    /// Cria o contexto de avaliação a partir das propriedades atuais.
    /// </summary>
    private RuleEvaluationContext CreateEvaluationContext()
    {
        var context = new RuleEvaluationContext();
        
        // Popula timestamps das propriedades persistidas
        context.SetLastTimeNormal(RuleType.Dryness, LastTimeAboveDryThreshold);
        context.SetLastTimeNormal(RuleType.ExtremeHeat, LastTimeBelowHeatThreshold);
        context.SetLastTimeNormal(RuleType.Frost, LastTimeAboveFrostThreshold);
        context.SetLastTimeNormal(RuleType.DryAir, LastTimeAboveDryAirThreshold);
        context.SetLastTimeNormal(RuleType.HumidAir, LastTimeBelowHumidAirThreshold);
        
        // Popula flags de alerta ativo
        foreach (var kvp in _activeAlerts)
        {
            context.SetAlertActive(kvp.Key, kvp.Value);
        }
        
        return context;
    }

    /// <summary>
    /// Aplica as mudanças do contexto de volta às propriedades.
    /// </summary>
    private void ApplyContextToProperties(RuleEvaluationContext context)
    {
        // Atualiza propriedades persistidas
        LastTimeAboveDryThreshold = context.GetLastTimeNormal(RuleType.Dryness);
        LastTimeBelowHeatThreshold = context.GetLastTimeNormal(RuleType.ExtremeHeat);
        LastTimeAboveFrostThreshold = context.GetLastTimeNormal(RuleType.Frost);
        LastTimeAboveDryAirThreshold = context.GetLastTimeNormal(RuleType.DryAir);
        LastTimeBelowHumidAirThreshold = context.GetLastTimeNormal(RuleType.HumidAir);

        // Atualiza dicionário de alertas ativos
        foreach (var kvp in context.ActiveAlerts)
        {
            _activeAlerts[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Atualiza os valores da última leitura.
    /// </summary>
    private void UpdateLastReadingValues(SensorReading reading)
    {
        SensorId = reading.SensorId;
        LastSoilMoisture = reading.SoilMoisture;
        LastSoilTemperature = reading.SoilTemperature;
        LastAirTemperature = reading.AirTemperature;
        LastAirHumidity = reading.AirHumidity;
        LastRain = reading.Rain;
        LastReadingAt = reading.Timestamp;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Aplica o resultado da avaliação de uma regra.
    /// </summary>
    private void ApplyEvaluationResult(RuleEvaluationResult result, AlertType alertType)
    {
        if (result.ShouldRaiseAlert && result.AlertReason != null)
        {
            RaiseAlert(alertType, result.AlertReason);
        }
        else if (result.ShouldResolveAlert)
        {
            ResolveAlert(alertType);
        }
    }

    /// <summary>
    /// Cria um novo alerta usando a factory unificada.
    /// Garante invariante: só pode existir 1 alerta ativo por tipo.
    /// </summary>
    private void RaiseAlert(AlertType type, string reason)
    {
        if (_alerts.Any(a => a.AlertType == type && a.Status == AlertStatus.Active))
            return;

        var alert = Alert.Create(type, FarmId, FieldId, reason);
        _alerts.Add(alert);
    }

    /// <summary>
    /// Resolve um alerta ativo de um tipo específico.
    /// </summary>
    private void ResolveAlert(AlertType type)
    {
        var activeAlert = _alerts.FirstOrDefault(a => a.AlertType == type && a.Status == AlertStatus.Active);
        activeAlert?.Resolve();
    }

    /// <summary>
    /// Atualiza o status do talhão baseado nos alertas ativos.
    /// Usa severidade do AlertType para priorizar (menor = mais crítico).
    /// </summary>
    private void UpdateStatus()
    {
        // Busca o alerta ativo mais crítico (menor severidade)
        var mostCriticalAlert = _alerts
            .Where(a => a.Status == AlertStatus.Active)
            .OrderBy(a => a.AlertType.GetSeverity())
            .FirstOrDefault();

        if (mostCriticalAlert != null)
        {
            Status = mostCriticalAlert.AlertType.ToFieldStatus();
            StatusReason = mostCriticalAlert.Reason ?? mostCriticalAlert.AlertType.GetDefaultReason();
        }
        else
        {
            Status = FieldStatusType.Normal;
            StatusReason = "Condições dentro do esperado";
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
