using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Interface para avaliadores de regras de alerta.
/// Cada implementacao encapsula a logica de uma regra especifica.
/// </summary>
internal interface IRuleEvaluator
{
    RuleType RuleType { get; }
    AlertType AlertType { get; }

    /// <summary>
    /// Avalia uma leitura de sensor contra a regra configurada.
    /// </summary>
    RuleEvaluationResult Evaluate(SensorReading reading, Rule rule, RuleEvaluationContext context);
}
