using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Avaliador da regra de geada (Frost).
/// Gera alerta quando a temperatura do ar fica abaixo do threshold por tempo >= WindowHours.
/// </summary>
internal sealed class FrostRuleEvaluator : RuleEvaluatorBase
{
    public override RuleType RuleType => RuleType.Frost;
    public override AlertType AlertType => AlertType.Frost;

    public override RuleEvaluationResult Evaluate(SensorReading reading, Rule rule, RuleEvaluationContext context)
    {
        if (reading.AirTemperature == null)
            return RuleEvaluationResult.NoAction();

        Temperature thresholdTemp = Temperature.FromCelsius(rule.Threshold).Value!;
        var windowHours = rule.WindowHours;

        // Temperatura acima ou igual ao threshold = condição normal (strict: <)
        if (reading.AirTemperature.Celsius >= thresholdTemp.Celsius)
        {
            context.SetLastTimeNormal(RuleType, reading.Timestamp);

            if (context.IsAlertActive(AlertType))
            {
                context.SetAlertActive(AlertType, false);
                return RuleEvaluationResult.ResolveAlert();
            }

            return RuleEvaluationResult.NoAction();
        }

        // Temperatura abaixo do threshold (strict) - primeira leitura inicializa tracking
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
            var reason = $"Temperatura do ar abaixo de {thresholdTemp.Celsius}°C por {hoursBelow:F0} horas (risco de geada)";
            
            context.SetAlertActive(AlertType, true);
            return RuleEvaluationResult.RaiseAlert(reason);
        }

        return RuleEvaluationResult.NoAction();
    }
}
