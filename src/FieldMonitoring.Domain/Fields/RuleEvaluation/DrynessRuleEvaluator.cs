using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Avaliador da regra de seca (Dryness).
/// Gera alerta quando a umidade do solo fica abaixo do threshold por tempo >= WindowHours.
/// </summary>
internal sealed class DrynessRuleEvaluator : RuleEvaluatorBase
{
    public override RuleType RuleType => RuleType.Dryness;
    public override AlertType AlertType => AlertType.Dryness;

    public override RuleEvaluationResult Evaluate(SensorReading reading, Rule rule, RuleEvaluationContext context)
    {
        SoilMoisture thresholdMoisture = SoilMoisture.FromPercent(rule.Threshold).Value!;
        var windowHours = rule.WindowHours;

        // Umidade acima ou igual ao threshold = condição normal
        if (reading.SoilMoisture.IsAbove(thresholdMoisture) || 
            reading.SoilMoisture.Percent == thresholdMoisture.Percent)
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
        if (context.GetLastTimeNormal(RuleType) == null)
        {
            context.SetLastTimeNormal(RuleType, reading.Timestamp);
        }

        // Verifica se excedeu a janela de tempo
        var lastTimeNormal = context.GetLastTimeNormal(RuleType);
        if (IsConditionExceeded(lastTimeNormal, windowHours, reading.Timestamp) && 
            !context.IsAlertActive(AlertType))
        {
            var hoursBelow = (reading.Timestamp - lastTimeNormal!.Value).TotalHours;
            var reason = $"Umidade do solo abaixo de {thresholdMoisture.Percent}% por {hoursBelow:F0} horas";
            
            context.SetAlertActive(AlertType, true);
            return RuleEvaluationResult.RaiseAlert(reason);
        }

        return RuleEvaluationResult.NoAction();
    }
}
