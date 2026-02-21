namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Resultado imutavel da avaliacao de uma regra.
/// </summary>
internal sealed record RuleEvaluationResult
{
    /// <summary>
    /// Indica se deve criar um novo alerta.
    /// </summary>
    public bool ShouldRaiseAlert { get; init; }

    /// <summary>
    /// Indica se deve resolver o alerta ativo.
    /// </summary>
    public bool ShouldResolveAlert { get; init; }

    /// <summary>
    /// Motivo descritivo do alerta (quando aplicável).
    /// </summary>
    public string? AlertReason { get; init; }

    public static RuleEvaluationResult NoAction() => new();

    public static RuleEvaluationResult RaiseAlert(string reason) => new()
    {
        ShouldRaiseAlert = true,
        AlertReason = reason
    };

    public static RuleEvaluationResult ResolveAlert() => new()
    {
        ShouldResolveAlert = true
    };
}
