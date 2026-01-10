namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Contexto passado para os avaliadores de regras.
/// Contem os timestamps e flags de alerta necessarios para avaliacao.
/// </summary>
internal sealed class RuleEvaluationContext
{
    // Timestamps de quando a condicao estava normal
    public DateTime? LastTimeAboveDryThreshold { get; set; }
    public DateTime? LastTimeBelowHeatThreshold { get; set; }
    public DateTime? LastTimeAboveFrostThreshold { get; set; }
    public DateTime? LastTimeAboveDryAirThreshold { get; set; }
    public DateTime? LastTimeBelowHumidAirThreshold { get; set; }

    // Flags de alerta ativo
    public bool DryAlertActive { get; set; }
    public bool HeatAlertActive { get; set; }
    public bool FrostAlertActive { get; set; }
    public bool DryAirAlertActive { get; set; }
    public bool HumidAirAlertActive { get; set; }
}
