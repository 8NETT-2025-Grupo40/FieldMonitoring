using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Avaliador da regra de geada (Frost).
/// </summary>
internal sealed class FrostRuleEvaluator : RuleEvaluatorBase
{
    public override RuleType RuleType => RuleType.Frost;
    public override AlertType AlertType => AlertType.Frost;

    protected override bool TryGetSensorValue(SensorReading reading, out double sensorValue)
    {
        if (reading.AirTemperature == null)
        {
            sensorValue = default;
            return false;
        }

        sensorValue = reading.AirTemperature.Celsius;
        return true;
    }

    protected override void ValidateThreshold(double threshold)
    {
        var result = Temperature.FromCelsius(threshold);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Threshold inválido para regra de geada: {result.Error}");
    }

    protected override bool IsConditionNormal(double sensorValue, double threshold)
        => sensorValue >= threshold;

    protected override string BuildAlertReason(double threshold, double hoursInCondition)
        => $"Temperatura do ar abaixo de {threshold}°C por {hoursInCondition:F0} horas (risco de geada)";
}
