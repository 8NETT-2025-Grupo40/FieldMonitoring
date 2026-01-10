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
    public DateTime? LastReadingAt { get; private set; }
    public SoilMoisture? LastSoilMoisture { get; private set; }
    public Temperature? LastSoilTemperature { get; private set; }
    public Temperature? LastAirTemperature { get; private set; }
    public AirHumidity? LastAirHumidity { get; private set; }
    public RainMeasurement? LastRain { get; private set; }
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Estado das regras - propriedades persistidas pelo EF Core
    public DateTime? LastTimeAboveDryThreshold { get; private set; }
    public DateTime? LastTimeBelowHeatThreshold { get; private set; }
    public DateTime? LastTimeAboveFrostThreshold { get; private set; }
    public DateTime? LastTimeAboveDryAirThreshold { get; private set; }
    public DateTime? LastTimeBelowHumidAirThreshold { get; private set; }

    // Flags de alerta ativo (in-memory, sincronizadas via SyncAlertStates)
    private bool _dryAlertActive;
    private bool _heatAlertActive;
    private bool _frostAlertActive;
    private bool _dryAirAlertActive;
    private bool _humidAirAlertActive;

    // Avaliadores de regras (Strategy Pattern) - compartilhados entre instancias
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
        FieldId = fieldId ?? throw new ArgumentNullException(nameof(fieldId));
        FarmId = farmId ?? throw new ArgumentNullException(nameof(farmId));
    }

    /// <summary>
    /// Cria um novo talhão.
    /// </summary>
    public static Field Create(string fieldId, string farmId)
    {
        return new Field(fieldId, farmId);
    }

    /// <summary>
    /// Sincroniza o estado interno dos alertas com os alertas carregados.
    /// Chamado pelo Repository após EF carregar os alertas via Include.
    /// </summary>
    public void SyncAlertStates()
    {
        _dryAlertActive = _alerts.Any(a =>
            a.AlertType == AlertType.Dryness &&
            a.Status == AlertStatus.Active);
        _heatAlertActive = _alerts.Any(a =>
            a.AlertType == AlertType.ExtremeHeat &&
            a.Status == AlertStatus.Active);
        _frostAlertActive = _alerts.Any(a =>
            a.AlertType == AlertType.Frost &&
            a.Status == AlertStatus.Active);
        _dryAirAlertActive = _alerts.Any(a =>
            a.AlertType == AlertType.DryAir &&
            a.Status == AlertStatus.Active);
        _humidAirAlertActive = _alerts.Any(a =>
            a.AlertType == AlertType.HumidAir &&
            a.Status == AlertStatus.Active);
    }

    /// <summary>
    /// Mantém compatibilidade com código existente.
    /// </summary>
    public void SyncDryAlertState() => SyncAlertStates();

    /// <summary>
    /// Carrega alertas ativos no aggregate (usado quando não há Include do EF).
    /// </summary>
    public void LoadAlerts(IEnumerable<Alert> alerts)
    {
        _alerts.Clear();
        _alerts.AddRange(alerts);
        SyncAlertStates();
    }

    /// <summary>
    /// Processa uma nova leitura de sensor.
    /// Atualiza estado, avalia regras e gerencia alertas.
    /// </summary>
    public void ProcessReading(SensorReading reading, IReadOnlyList<Rule> rules)
    {
        if (reading.FieldId != FieldId)
            throw new InvalidOperationException($"Reading pertence a outro talhão: {reading.FieldId}");

        // Atualiza valores da última leitura
        UpdateLastReadingValues(reading);

        // Cria contexto para avaliação das regras
        var context = CreateEvaluationContext();

        // Avalia todas as regras habilitadas usando os evaluators
        foreach (var rule in rules.Where(r => r.IsEnabled))
        {
            if (Evaluators.TryGetValue(rule.RuleType, out var evaluator))
            {
                var result = evaluator.Evaluate(reading, rule, context);
                ApplyEvaluationResult(result, evaluator.AlertType);
            }
        }

        // Sincroniza propriedades persistidas com o contexto atualizado
        ApplyContextToProperties(context);

        // Atualiza status baseado em alertas ativos
        UpdateStatus();
    }

    /// <summary>
    /// Sobrecarga para manter compatibilidade com código existente.
    /// </summary>
    public void ProcessReading(SensorReading reading, Rule drynessRule)
    {
        ProcessReading(reading, new List<Rule> { drynessRule });
    }

    /// <summary>
    /// Cria o contexto de avaliação a partir das propriedades atuais.
    /// </summary>
    private RuleEvaluationContext CreateEvaluationContext()
    {
        return new RuleEvaluationContext
        {
            LastTimeAboveDryThreshold = LastTimeAboveDryThreshold,
            LastTimeBelowHeatThreshold = LastTimeBelowHeatThreshold,
            LastTimeAboveFrostThreshold = LastTimeAboveFrostThreshold,
            LastTimeAboveDryAirThreshold = LastTimeAboveDryAirThreshold,
            LastTimeBelowHumidAirThreshold = LastTimeBelowHumidAirThreshold,
            DryAlertActive = _dryAlertActive,
            HeatAlertActive = _heatAlertActive,
            FrostAlertActive = _frostAlertActive,
            DryAirAlertActive = _dryAirAlertActive,
            HumidAirAlertActive = _humidAirAlertActive
        };
    }

    /// <summary>
    /// Aplica as mudanças do contexto de volta às propriedades.
    /// </summary>
    private void ApplyContextToProperties(RuleEvaluationContext context)
    {
        LastTimeAboveDryThreshold = context.LastTimeAboveDryThreshold;
        LastTimeBelowHeatThreshold = context.LastTimeBelowHeatThreshold;
        LastTimeAboveFrostThreshold = context.LastTimeAboveFrostThreshold;
        LastTimeAboveDryAirThreshold = context.LastTimeAboveDryAirThreshold;
        LastTimeBelowHumidAirThreshold = context.LastTimeBelowHumidAirThreshold;

        _dryAlertActive = context.DryAlertActive;
        _heatAlertActive = context.HeatAlertActive;
        _frostAlertActive = context.FrostAlertActive;
        _dryAirAlertActive = context.DryAirAlertActive;
        _humidAirAlertActive = context.HumidAirAlertActive;
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
        UpdatedAt = DateTime.UtcNow;
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
    /// Cria um novo alerta.
    /// Garante invariante: só pode existir 1 alerta ativo por tipo.
    /// </summary>
    private void RaiseAlert(AlertType type, string reason)
    {
        // Invariante: não permite alertas duplicados do mesmo tipo
        if (_alerts.Any(a => a.AlertType == type && a.Status == AlertStatus.Active))
            return;

        Alert alert = type switch
        {
            AlertType.Dryness => Alert.CreateDrynessAlert(FarmId, FieldId, reason),
            AlertType.ExtremeHeat => Alert.CreateExtremeHeatAlert(FarmId, FieldId, reason),
            AlertType.Frost => Alert.CreateFrostAlert(FarmId, FieldId, reason),
            AlertType.DryAir => Alert.CreateDryAirAlert(FarmId, FieldId, reason),
            AlertType.HumidAir => Alert.CreateHumidAirAlert(FarmId, FieldId, reason),
            _ => Alert.CreateDrynessAlert(FarmId, FieldId, reason)
        };

        _alerts.Add(alert);
    }

    /// <summary>
    /// Resolve um alerta ativo de um tipo específico.
    /// </summary>
    private void ResolveAlert(AlertType type)
    {
        Alert? activeAlert = _alerts.FirstOrDefault(a => a.AlertType == type && a.Status == AlertStatus.Active);
        activeAlert?.Resolve();
    }

    /// <summary>
    /// Atualiza o status do talhão baseado nos alertas ativos.
    /// Prioriza alertas por severidade.
    /// </summary>
    private void UpdateStatus()
    {
        // Prioriza alertas por severidade (geada é mais crítica)
        if (_frostAlertActive)
        {
            Alert? alert = _alerts.FirstOrDefault(a => a.AlertType == AlertType.Frost && a.Status == AlertStatus.Active);
            Status = FieldStatusType.FrostAlert;
            StatusReason = alert?.Reason ?? "Alerta de geada ativo";
        }
        else if (_heatAlertActive)
        {
            Alert? alert = _alerts.FirstOrDefault(a => a.AlertType == AlertType.ExtremeHeat && a.Status == AlertStatus.Active);
            Status = FieldStatusType.HeatAlert;
            StatusReason = alert?.Reason ?? "Alerta de calor extremo ativo";
        }
        else if (_dryAlertActive)
        {
            Alert? alert = _alerts.FirstOrDefault(a => a.AlertType == AlertType.Dryness && a.Status == AlertStatus.Active);
            Status = FieldStatusType.DryAlert;
            StatusReason = alert?.Reason ?? "Alerta de seca ativo";
        }
        else if (_dryAirAlertActive)
        {
            Alert? alert = _alerts.FirstOrDefault(a => a.AlertType == AlertType.DryAir && a.Status == AlertStatus.Active);
            Status = FieldStatusType.DryAirAlert;
            StatusReason = alert?.Reason ?? "Alerta de ar seco ativo";
        }
        else if (_humidAirAlertActive)
        {
            Alert? alert = _alerts.FirstOrDefault(a => a.AlertType == AlertType.HumidAir && a.Status == AlertStatus.Active);
            Status = FieldStatusType.HumidAirAlert;
            StatusReason = alert?.Reason ?? "Alerta de ar úmido ativo";
        }
        else
        {
            Status = FieldStatusType.Normal;
            StatusReason = "Condições dentro do esperado";
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
