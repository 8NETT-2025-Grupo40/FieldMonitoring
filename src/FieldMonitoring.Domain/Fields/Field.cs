using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Fields.RuleEvaluation;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields;

/// <summary>
/// Aggregate Root que representa um Talhao (Field).
/// Garante invariantes: so 1 alerta ativo por tipo, transicoes de status validas.
/// </summary>
public class Field
{
    public string FieldId { get; private set; }
    public string FarmId { get; private set; }
    public string? SensorId { get; private set; }

    /// <summary>
    /// Status operacional derivado dos alertas ativos.
    /// </summary>
    public FieldStatusType Status { get; private set; } = FieldStatusType.Normal;

    /// <summary>
    /// Descrição legível do motivo do status atual.
    /// </summary>
    public string? StatusReason { get; private set; }

    public DateTimeOffset? LastReadingAt { get; private set; }
    public SoilMoisture? LastSoilMoisture { get; private set; }
    public Temperature? LastSoilTemperature { get; private set; }
    public Temperature? LastAirTemperature { get; private set; }
    public AirHumidity? LastAirHumidity { get; private set; }
    public RainMeasurement? LastRain { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // Timestamps de quando cada regra estava em condicao normal (persistidos pelo EF Core).
    // Nao renomear: mapeados diretamente para colunas existentes no banco.
    public DateTimeOffset? LastTimeAboveDryThreshold { get; private set; }
    public DateTimeOffset? LastTimeBelowHeatThreshold { get; private set; }
    public DateTimeOffset? LastTimeAboveFrostThreshold { get; private set; }
    public DateTimeOffset? LastTimeAboveDryAirThreshold { get; private set; }
    public DateTimeOffset? LastTimeBelowHumidAirThreshold { get; private set; }

    private readonly Dictionary<AlertType, bool> _activeAlerts = new();

    private static readonly Dictionary<RuleType, IRuleEvaluator> Evaluators = new()
    {
        [RuleType.Dryness] = new DrynessRuleEvaluator(),
        [RuleType.ExtremeHeat] = new ExtremeHeatRuleEvaluator(),
        [RuleType.Frost] = new FrostRuleEvaluator(),
        [RuleType.DryAir] = new DryAirRuleEvaluator(),
        [RuleType.HumidAir] = new HumidAirRuleEvaluator()
    };

    private readonly List<Alert> _alerts = new();
    public IReadOnlyList<Alert> Alerts => _alerts.AsReadOnly();

    private Field(string fieldId, string farmId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);
        ArgumentException.ThrowIfNullOrWhiteSpace(farmId);

        FieldId = fieldId;
        FarmId = farmId;
    }

    public static Field Create(string fieldId, string farmId)
    {
        return new Field(fieldId, farmId);
    }

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
    /// </summary>
    public void RehydrateAlerts(IEnumerable<Alert> alerts)
    {
        ArgumentNullException.ThrowIfNull(alerts);

        _alerts.Clear();
        _alerts.AddRange(alerts);
        Rehydrate();
    }

    /// <summary>
    /// Processa uma nova leitura de sensor: atualiza estado, avalia regras e gerencia alertas.
    /// Retorna false quando a leitura esta fora de ordem temporal.
    /// </summary>
    public bool ProcessReading(SensorReading reading, IReadOnlyList<Rule> rules)
    {
        if (reading.FieldId != FieldId)
            throw new InvalidOperationException($"Leitura pertence a outro talhão: {reading.FieldId}");

        if (reading.FarmId != FarmId)
            throw new InvalidOperationException($"Leitura pertence a outra fazenda: {reading.FarmId}");

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

    private RuleEvaluationContext CreateEvaluationContext()
    {
        var context = new RuleEvaluationContext();
        
        context.SetLastTimeNormal(RuleType.Dryness, LastTimeAboveDryThreshold);
        context.SetLastTimeNormal(RuleType.ExtremeHeat, LastTimeBelowHeatThreshold);
        context.SetLastTimeNormal(RuleType.Frost, LastTimeAboveFrostThreshold);
        context.SetLastTimeNormal(RuleType.DryAir, LastTimeAboveDryAirThreshold);
        context.SetLastTimeNormal(RuleType.HumidAir, LastTimeBelowHumidAirThreshold);
        
        foreach (var kvp in _activeAlerts)
        {
            context.SetAlertActive(kvp.Key, kvp.Value);
        }
        
        return context;
    }

    private void ApplyContextToProperties(RuleEvaluationContext context)
    {
        LastTimeAboveDryThreshold = context.GetLastTimeNormal(RuleType.Dryness);
        LastTimeBelowHeatThreshold = context.GetLastTimeNormal(RuleType.ExtremeHeat);
        LastTimeAboveFrostThreshold = context.GetLastTimeNormal(RuleType.Frost);
        LastTimeAboveDryAirThreshold = context.GetLastTimeNormal(RuleType.DryAir);
        LastTimeBelowHumidAirThreshold = context.GetLastTimeNormal(RuleType.HumidAir);

        foreach (var kvp in context.ActiveAlerts)
        {
            _activeAlerts[kvp.Key] = kvp.Value;
        }
    }

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

    // Garante invariante: so pode existir 1 alerta ativo por tipo
    private void RaiseAlert(AlertType type, string reason)
    {
        if (_alerts.Any(a => a.AlertType == type && a.Status == AlertStatus.Active))
            return;

        var alert = Alert.Create(type, FarmId, FieldId, reason);
        _alerts.Add(alert);
    }

    private void ResolveAlert(AlertType type)
    {
        var activeAlert = _alerts.FirstOrDefault(a => a.AlertType == type && a.Status == AlertStatus.Active);
        activeAlert?.Resolve();
    }

    // Usa severidade do AlertType para priorizar (menor = mais critico)
    private void UpdateStatus()
    {
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
