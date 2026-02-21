using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Avaliador da regra de calor extremo (ExtremeHeat).
/// </summary>
internal sealed class ExtremeHeatRuleEvaluator : RuleEvaluatorBase
{
    public override RuleType RuleType => RuleType.ExtremeHeat;
    public override AlertType AlertType => AlertType.ExtremeHeat;

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
            throw new InvalidOperationException($"Threshold inválido para regra de calor extremo: {result.Error}");
    }

    protected override bool IsConditionNormal(double sensorValue, double threshold)
        => sensorValue <= threshold;

    protected override string BuildAlertReason(double threshold, double hoursInCondition)
        => $"Temperatura do ar acima de {threshold}°C por {hoursInCondition:F0} horas";
}
