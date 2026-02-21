using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Contexto mutavel passado para os avaliadores de regras.
/// Usa dicionarios genericos para nao precisar adicionar propriedades a cada nova regra.
/// </summary>
internal sealed class RuleEvaluationContext
{
    /// <summary>
    /// Última vez que cada regra estava em condição normal.
    /// </summary>
    public Dictionary<RuleType, DateTimeOffset?> LastTimeNormal { get; } = new();

    /// <summary>
    /// Estado de alertas ativos por tipo.
    /// </summary>
    public Dictionary<AlertType, bool> ActiveAlerts { get; } = new();

    public DateTimeOffset? GetLastTimeNormal(RuleType ruleType)
    {
        return LastTimeNormal.TryGetValue(ruleType, out var timestamp) ? timestamp : null;
    }

    public void SetLastTimeNormal(RuleType ruleType, DateTimeOffset? timestamp)
    {
        LastTimeNormal[ruleType] = timestamp;
    }

    public bool IsAlertActive(AlertType alertType)
    {
        return ActiveAlerts.TryGetValue(alertType, out var isActive) && isActive;
    }

    public void SetAlertActive(AlertType alertType, bool isActive)
    {
        ActiveAlerts[alertType] = isActive;
    }
}
