namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Resultado imutável da avaliação de uma regra.
/// Indica se deve criar um novo alerta, resolver um existente, ou nenhuma ação.
/// </summary>
internal sealed record RuleEvaluationResult
{
    /// <summary>
    /// Indica se um novo alerta deve ser criado.
    /// </summary>
    public bool ShouldRaiseAlert { get; init; }

    /// <summary>
    /// Indica se o alerta ativo deve ser resolvido.
    /// </summary>
    public bool ShouldResolveAlert { get; init; }

    /// <summary>
    /// Razão/motivo do alerta (usado quando ShouldRaiseAlert = true).
    /// </summary>
    public string? AlertReason { get; init; }

    /// <summary>
    /// Cria um resultado indicando que nenhuma ação é necessária.
    /// </summary>
    public static RuleEvaluationResult NoAction() => new();

    /// <summary>
    /// Cria um resultado indicando que um alerta deve ser criado.
    /// </summary>
    /// <param name="reason">Motivo do alerta.</param>
    public static RuleEvaluationResult RaiseAlert(string reason) => new()
    {
        ShouldRaiseAlert = true,
        AlertReason = reason
    };

    /// <summary>
    /// Cria um resultado indicando que o alerta ativo deve ser resolvido.
    /// </summary>
    public static RuleEvaluationResult ResolveAlert() => new()
    {
        ShouldResolveAlert = true
    };
}
