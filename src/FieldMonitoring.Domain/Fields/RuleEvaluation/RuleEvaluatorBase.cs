using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Classe base para avaliadores de regras (Template Method).
/// O algoritmo de avaliação é fixo; subclasses fornecem apenas os detalhes
/// que variam entre regras: extração do sensor, validação do threshold,
/// direção da comparação e texto do alerta.
/// </summary>
internal abstract class RuleEvaluatorBase : IRuleEvaluator
{
    public abstract RuleType RuleType { get; }
    public abstract AlertType AlertType { get; }

    public RuleEvaluationResult Evaluate(
        SensorReading reading,
        Rule rule,
        RuleEvaluationContext context)
    {
        if (!TryGetSensorValue(reading, out var sensorValue))
            return RuleEvaluationResult.NoAction();

        ValidateThreshold(rule.Threshold);

        var threshold = rule.Threshold;
        var windowHours = rule.WindowHours;

        if (IsConditionNormal(sensorValue, threshold))
        {
            context.SetLastTimeNormal(RuleType, reading.Timestamp);

            if (context.IsAlertActive(AlertType))
            {
                context.SetAlertActive(AlertType, false);
                return RuleEvaluationResult.ResolveAlert();
            }

            return RuleEvaluationResult.NoAction();
        }

        // Inicia tracking da condição
        var lastTimeNormal = context.GetLastTimeNormal(RuleType);
        if (lastTimeNormal == null)
        {
            context.SetLastTimeNormal(RuleType, reading.Timestamp);
            lastTimeNormal = reading.Timestamp;
        }

        if (IsConditionExceeded(lastTimeNormal, windowHours, reading.Timestamp) &&
            !context.IsAlertActive(AlertType))
        {
            var hoursInCondition = (reading.Timestamp - lastTimeNormal!.Value).TotalHours;
            var reason = BuildAlertReason(threshold, hoursInCondition);

            context.SetAlertActive(AlertType, true);
            return RuleEvaluationResult.RaiseAlert(reason);
        }

        return RuleEvaluationResult.NoAction();
    }

    protected abstract bool TryGetSensorValue(SensorReading reading, out double sensorValue);

    protected abstract void ValidateThreshold(double threshold);

    protected abstract bool IsConditionNormal(double sensorValue, double threshold);

    protected abstract string BuildAlertReason(double threshold, double hoursInCondition);

    protected static bool IsConditionExceeded(DateTimeOffset? lastTimeNormal, int windowHours, DateTimeOffset currentTime)
    {
        if (lastTimeNormal == null)
            return false;

        // Leitura atual mais antiga que última vez normal (dados históricos)
        if (currentTime < lastTimeNormal.Value)
            return false;

        TimeSpan elapsed = currentTime - lastTimeNormal.Value;
        return elapsed.TotalHours >= windowHours;
    }
}
