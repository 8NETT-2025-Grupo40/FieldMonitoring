using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Avaliador da regra de ar seco (DryAir).
/// Gera alerta quando a umidade do ar fica abaixo do threshold por tempo >= WindowHours.
/// </summary>
internal sealed class DryAirRuleEvaluator : RuleEvaluatorBase
{
    public override RuleType RuleType => RuleType.DryAir;
    public override AlertType AlertType => AlertType.DryAir;

    public override RuleEvaluationResult Evaluate(SensorReading reading, Rule rule, RuleEvaluationContext context)
    {
        if (reading.AirHumidity == null)
            return RuleEvaluationResult.NoAction();

        AirHumidity thresholdHumidity = AirHumidity.FromPercent(rule.Threshold).Value!;
        var windowHours = rule.WindowHours;

        // Umidade acima ou igual ao threshold = condicao normal
        if (reading.AirHumidity.IsAbove(thresholdHumidity) || 
            reading.AirHumidity.Percent == thresholdHumidity.Percent)
        {
            context.LastTimeAboveDryAirThreshold = reading.Timestamp;

            if (context.DryAirAlertActive)
            {
                context.DryAirAlertActive = false;
                return RuleEvaluationResult.ResolveAlert();
            }

            return RuleEvaluationResult.NoAction();
        }

        // Umidade abaixo do threshold - primeira leitura inicializa tracking
        if (context.LastTimeAboveDryAirThreshold == null)
        {
            context.LastTimeAboveDryAirThreshold = reading.Timestamp;
        }

        // Verifica se excedeu a janela de tempo
        if (IsConditionExceeded(context.LastTimeAboveDryAirThreshold, windowHours, reading.Timestamp) && 
            !context.DryAirAlertActive)
        {
            var hoursBelow = (reading.Timestamp - context.LastTimeAboveDryAirThreshold!.Value).TotalHours;
            var reason = $"Umidade do ar abaixo de {thresholdHumidity.Percent}% por {hoursBelow:F0} horas";
            
            context.DryAirAlertActive = true;
            return RuleEvaluationResult.RaiseAlert(reason);
        }

        return RuleEvaluationResult.NoAction();
    }
}
