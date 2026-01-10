using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Avaliador da regra de ar umido (HumidAir).
/// Gera alerta quando a umidade do ar fica acima do threshold por tempo >= WindowHours.
/// </summary>
internal sealed class HumidAirRuleEvaluator : RuleEvaluatorBase
{
    public override RuleType RuleType => RuleType.HumidAir;
    public override AlertType AlertType => AlertType.HumidAir;

    public override RuleEvaluationResult Evaluate(SensorReading reading, Rule rule, RuleEvaluationContext context)
    {
        if (reading.AirHumidity == null)
            return RuleEvaluationResult.NoAction();

        AirHumidity thresholdHumidity = AirHumidity.FromPercent(rule.Threshold).Value!;
        var windowHours = rule.WindowHours;

        // Umidade abaixo ou igual ao threshold = condicao normal
        if (reading.AirHumidity.IsBelow(thresholdHumidity) || 
            reading.AirHumidity.Percent == thresholdHumidity.Percent)
        {
            context.LastTimeBelowHumidAirThreshold = reading.Timestamp;

            if (context.HumidAirAlertActive)
            {
                context.HumidAirAlertActive = false;
                return RuleEvaluationResult.ResolveAlert();
            }

            return RuleEvaluationResult.NoAction();
        }

        // Umidade acima do threshold - primeira leitura inicializa tracking
        if (context.LastTimeBelowHumidAirThreshold == null)
        {
            context.LastTimeBelowHumidAirThreshold = reading.Timestamp;
        }

        // Verifica se excedeu a janela de tempo
        if (IsConditionExceeded(context.LastTimeBelowHumidAirThreshold, windowHours, reading.Timestamp) && 
            !context.HumidAirAlertActive)
        {
            var hoursAbove = (reading.Timestamp - context.LastTimeBelowHumidAirThreshold!.Value).TotalHours;
            var reason = $"Umidade do ar acima de {thresholdHumidity.Percent}% por {hoursAbove:F0} horas";
            
            context.HumidAirAlertActive = true;
            return RuleEvaluationResult.RaiseAlert(reason);
        }

        return RuleEvaluationResult.NoAction();
    }
}
