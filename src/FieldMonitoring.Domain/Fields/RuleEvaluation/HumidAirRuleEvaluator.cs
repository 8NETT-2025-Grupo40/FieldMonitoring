using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Avaliador da regra de ar úmido (HumidAir).
/// </summary>
internal sealed class HumidAirRuleEvaluator : RuleEvaluatorBase
{
    public override RuleType RuleType => RuleType.HumidAir;
    public override AlertType AlertType => AlertType.HumidAir;

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
            throw new InvalidOperationException($"Threshold inválido para regra de ar úmido: {result.Error}");
    }

    protected override bool IsConditionNormal(double sensorValue, double threshold)
        => sensorValue <= threshold;

    protected override string BuildAlertReason(double threshold, double hoursInCondition)
        => $"Umidade do ar acima de {threshold}% por {hoursInCondition:F0} horas";
}
