using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Contexto passado para os avaliadores de regras.
/// Usa Dictionaries genéricos para evitar adicionar propriedades para cada nova regra.
/// </summary>
internal sealed class RuleEvaluationContext
{
    /// <summary>
    /// Timestamps de quando cada regra estava em condição normal.
    /// Chave: RuleType, Valor: última vez que a condição estava OK.
    /// </summary>
    public Dictionary<RuleType, DateTimeOffset?> LastTimeNormal { get; } = new();

    /// <summary>
    /// Flags indicando se cada tipo de alerta está ativo.
    /// Chave: AlertType, Valor: true se alerta ativo.
    /// </summary>
    public Dictionary<AlertType, bool> ActiveAlerts { get; } = new();

    /// <summary>
    /// Obtém o timestamp da última vez que a regra estava normal.
    /// </summary>
    public DateTimeOffset? GetLastTimeNormal(RuleType ruleType)
    {
        return LastTimeNormal.TryGetValue(ruleType, out var timestamp) ? timestamp : null;
    }

    /// <summary>
    /// Define o timestamp da última vez que a regra estava normal.
    /// </summary>
    public void SetLastTimeNormal(RuleType ruleType, DateTimeOffset? timestamp)
    {
        LastTimeNormal[ruleType] = timestamp;
    }

    /// <summary>
    /// Verifica se um tipo de alerta está ativo.
    /// </summary>
    public bool IsAlertActive(AlertType alertType)
    {
        return ActiveAlerts.TryGetValue(alertType, out var isActive) && isActive;
    }

    /// <summary>
    /// Define se um tipo de alerta está ativo.
    /// </summary>
    public void SetAlertActive(AlertType alertType, bool isActive)
    {
        ActiveAlerts[alertType] = isActive;
    }
}
