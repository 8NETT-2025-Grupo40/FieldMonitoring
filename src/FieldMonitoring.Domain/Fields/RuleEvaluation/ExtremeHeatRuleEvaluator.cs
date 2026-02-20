using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Avaliador da regra de calor extremo (ExtremeHeat).
/// Gera alerta quando a temperatura do ar fica acima do threshold por tempo >= WindowHours.
/// </summary>
internal sealed class ExtremeHeatRuleEvaluator : RuleEvaluatorBase
{
    public override RuleType RuleType => RuleType.ExtremeHeat;
    public override AlertType AlertType => AlertType.ExtremeHeat;

    public override RuleEvaluationResult Evaluate(SensorReading reading, Rule rule, RuleEvaluationContext context)
    {
        if (reading.AirTemperature == null)
            return RuleEvaluationResult.NoAction();

        var thresholdResult = Temperature.FromCelsius(rule.Threshold);
        if (!thresholdResult.IsSuccess)
            throw new InvalidOperationException($"Threshold inválido para regra de calor extremo: {thresholdResult.Error}");

        Temperature thresholdTemp = thresholdResult.Value!;
        var windowHours = rule.WindowHours;

        // Temperatura abaixo ou igual ao threshold = condição normal (strict: >)
        if (reading.AirTemperature.Celsius <= thresholdTemp.Celsius)
        {
            context.SetLastTimeNormal(RuleType, reading.Timestamp);

            if (context.IsAlertActive(AlertType))
            {
                context.SetAlertActive(AlertType, false);
                return RuleEvaluationResult.ResolveAlert();
            }

            return RuleEvaluationResult.NoAction();
        }

        // Temperatura acima do threshold (strict) - primeira leitura inicializa tracking
        var lastTimeNormal = context.GetLastTimeNormal(RuleType);
        if (lastTimeNormal == null)
        {
            context.SetLastTimeNormal(RuleType, reading.Timestamp);
            lastTimeNormal = reading.Timestamp;
        }

        // Verifica se excedeu a janela de tempo
        if (IsConditionExceeded(lastTimeNormal, windowHours, reading.Timestamp) && 
            !context.IsAlertActive(AlertType))
        {
            var hoursAbove = (reading.Timestamp - lastTimeNormal!.Value).TotalHours;
            var reason = $"Temperatura do ar acima de {thresholdTemp.Celsius}°C por {hoursAbove:F0} horas";
            
            context.SetAlertActive(AlertType, true);
            return RuleEvaluationResult.RaiseAlert(reason);
        }

        return RuleEvaluationResult.NoAction();
    }
}
