using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Fields.RuleEvaluation;

/// <summary>
/// Avaliador da regra de seca (Dryness).
/// </summary>
internal sealed class DrynessRuleEvaluator : RuleEvaluatorBase
{
    public override RuleType RuleType => RuleType.Dryness;
    public override AlertType AlertType => AlertType.Dryness;

    protected override bool TryGetSensorValue(SensorReading reading, out double sensorValue)
    {
        sensorValue = reading.SoilMoisture.Percent;
        return true;
    }

    protected override void ValidateThreshold(double threshold)
    {
        var result = SoilMoisture.FromPercent(threshold);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Threshold inválido para regra de seca: {result.Error}");
    }

    protected override bool IsConditionNormal(double sensorValue, double threshold)
        => sensorValue >= threshold;

    protected override string BuildAlertReason(double threshold, double hoursInCondition)
        => $"Umidade do solo abaixo de {threshold}% por {hoursInCondition:F0} horas";
}
