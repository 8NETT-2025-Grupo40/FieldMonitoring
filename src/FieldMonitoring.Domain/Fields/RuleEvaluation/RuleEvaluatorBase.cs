using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Classe base para avaliadores de regras.
/// Fornece metodo utilitario compartilhado.
/// </summary>
internal abstract class RuleEvaluatorBase : IRuleEvaluator
{
    public abstract RuleType RuleType { get; }
    public abstract AlertType AlertType { get; }

    public abstract RuleEvaluationResult Evaluate(
        SensorReading reading, 
        Rule rule, 
        RuleEvaluationContext context);

    /// <summary>
    /// Verifica se a condicao anormal excedeu a janela de tempo configurada.
    /// </summary>
    protected static bool IsConditionExceeded(DateTime? lastTimeNormal, int windowHours, DateTime currentTime)
    {
        if (lastTimeNormal == null)
            return false;

        // Leitura atual mais antiga que ultima vez normal (dados historicos)
        if (currentTime < lastTimeNormal.Value)
            return false;

        TimeSpan elapsed = currentTime - lastTimeNormal.Value;
        return elapsed.TotalHours >= windowHours;
    }
}
