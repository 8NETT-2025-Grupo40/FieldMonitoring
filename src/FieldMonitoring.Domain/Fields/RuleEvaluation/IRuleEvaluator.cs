using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Interface interna para avaliadores de regras de alerta.
/// Cada implementacao encapsula a logica de uma regra especifica (Dryness, ExtremeHeat, etc).
/// </summary>
internal interface IRuleEvaluator
{
    /// <summary>
    /// Tipo de regra que este avaliador processa.
    /// </summary>
    RuleType RuleType { get; }

    /// <summary>
    /// Tipo de alerta gerado por este avaliador.
    /// </summary>
    AlertType AlertType { get; }

    /// <summary>
    /// Avalia uma leitura de sensor contra a regra configurada.
    /// </summary>
    /// <param name="reading">Leitura do sensor a ser avaliada.</param>
    /// <param name="rule">Regra com threshold e janela de tempo.</param>
    /// <param name="context">Contexto com timestamps e flags de alerta.</param>
    /// <returns>Resultado indicando se deve criar/resolver alerta.</returns>
    RuleEvaluationResult Evaluate(SensorReading reading, Rule rule, RuleEvaluationContext context);
}
