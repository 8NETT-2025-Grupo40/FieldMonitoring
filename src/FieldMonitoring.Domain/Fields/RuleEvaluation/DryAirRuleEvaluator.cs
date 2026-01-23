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

        // Umidade acima ou igual ao threshold = condição normal
        if (reading.AirHumidity.IsAbove(thresholdHumidity) || 
            reading.AirHumidity.Percent == thresholdHumidity.Percent)
        {
            context.SetLastTimeNormal(RuleType, reading.Timestamp);

            if (context.IsAlertActive(AlertType))
            {
                context.SetAlertActive(AlertType, false);
                return RuleEvaluationResult.ResolveAlert();
            }

            return RuleEvaluationResult.NoAction();
        }

        // Umidade abaixo do threshold - primeira leitura inicializa tracking
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
            var hoursBelow = (reading.Timestamp - lastTimeNormal!.Value).TotalHours;
            var reason = $"Umidade do ar abaixo de {thresholdHumidity.Percent}% por {hoursBelow:F0} horas";
            
            context.SetAlertActive(AlertType, true);
            return RuleEvaluationResult.RaiseAlert(reason);
        }

        return RuleEvaluationResult.NoAction();
    }
}
