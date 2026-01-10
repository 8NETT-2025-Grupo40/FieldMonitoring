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

        Temperature thresholdTemp = Temperature.FromCelsius(rule.Threshold).Value!;
        var windowHours = rule.WindowHours;

        // Temperatura abaixo ou igual ao threshold = condicao normal (strict: >)
        if (reading.AirTemperature.Celsius <= thresholdTemp.Celsius)
        {
            context.LastTimeBelowHeatThreshold = reading.Timestamp;

            if (context.HeatAlertActive)
            {
                context.HeatAlertActive = false;
                return RuleEvaluationResult.ResolveAlert();
            }

            return RuleEvaluationResult.NoAction();
        }

        // Temperatura acima do threshold (strict) - primeira leitura inicializa tracking
        if (context.LastTimeBelowHeatThreshold == null)
        {
            context.LastTimeBelowHeatThreshold = reading.Timestamp;
        }

        // Verifica se excedeu a janela de tempo
        if (IsConditionExceeded(context.LastTimeBelowHeatThreshold, windowHours, reading.Timestamp) && 
            !context.HeatAlertActive)
        {
            var hoursAbove = (reading.Timestamp - context.LastTimeBelowHeatThreshold!.Value).TotalHours;
            var reason = $"Temperatura do ar acima de {thresholdTemp.Celsius}°C por {hoursAbove:F0} horas";
            
            context.HeatAlertActive = true;
            return RuleEvaluationResult.RaiseAlert(reason);
        }

        return RuleEvaluationResult.NoAction();
    }
}
