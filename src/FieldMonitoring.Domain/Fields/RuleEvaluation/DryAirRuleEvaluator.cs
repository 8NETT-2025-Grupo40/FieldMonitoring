using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Avaliador da regra de ar seco (DryAir).
/// </summary>
internal sealed class DryAirRuleEvaluator : RuleEvaluatorBase
{
    public override RuleType RuleType => RuleType.DryAir;
    public override AlertType AlertType => AlertType.DryAir;

    protected override bool TryGetSensorValue(SensorReading reading, out double sensorValue)
    {
        if (reading.AirHumidity == null)
        {
            sensorValue = default;
            return false;
        }

        sensorValue = reading.AirHumidity.Percent;
        return true;
    }

    protected override void ValidateThreshold(double threshold)
    {
        var result = AirHumidity.FromPercent(threshold);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Threshold inválido para regra de ar seco: {result.Error}");
    }

    protected override bool IsConditionNormal(double sensorValue, double threshold)
        => sensorValue >= threshold;

    protected override string BuildAlertReason(double threshold, double hoursInCondition)
        => $"Umidade do ar abaixo de {threshold}% por {hoursInCondition:F0} horas";
}
