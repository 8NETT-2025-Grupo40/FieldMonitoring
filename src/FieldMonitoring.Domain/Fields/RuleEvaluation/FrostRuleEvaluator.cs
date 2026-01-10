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

        // Temperatura acima ou igual ao threshold = condicao normal (strict: <)
        if (reading.AirTemperature.Celsius >= thresholdTemp.Celsius)
        {
            context.LastTimeAboveFrostThreshold = reading.Timestamp;

            if (context.FrostAlertActive)
            {
                context.FrostAlertActive = false;
                return RuleEvaluationResult.ResolveAlert();
            }

            return RuleEvaluationResult.NoAction();
        }

        // Temperatura abaixo do threshold (strict) - primeira leitura inicializa tracking
        if (context.LastTimeAboveFrostThreshold == null)
        {
            context.LastTimeAboveFrostThreshold = reading.Timestamp;
        }

        // Verifica se excedeu a janela de tempo
        if (IsConditionExceeded(context.LastTimeAboveFrostThreshold, windowHours, reading.Timestamp) && 
            !context.FrostAlertActive)
        {
            var hoursBelow = (reading.Timestamp - context.LastTimeAboveFrostThreshold!.Value).TotalHours;
            var reason = $"Temperatura do ar abaixo de {thresholdTemp.Celsius}°C por {hoursBelow:F0} horas (risco de geada)";
            
            context.FrostAlertActive = true;
            return RuleEvaluationResult.RaiseAlert(reason);
        }

        return RuleEvaluationResult.NoAction();
    }
}
