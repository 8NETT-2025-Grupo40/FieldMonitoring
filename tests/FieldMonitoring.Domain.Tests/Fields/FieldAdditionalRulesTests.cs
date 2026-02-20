using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Fields;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Tests.Fields;

public class FieldAdditionalRulesTests
{
    [Fact]
    public void EvaluateExtremeHeatRule_WhenConditionExceedsWindow_ShouldRaiseAndResolveAlert()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        var rules = CreateRules(RuleType.ExtremeHeat, threshold: 40.0, windowHours: 4);

        SensorReading baseline = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow.AddHours(-5), airTemperature: 35.0);
        field.ProcessReading(baseline, rules);

        SensorReading heatReading = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow, airTemperature: 42.0);

        // Act
        field.ProcessReading(heatReading, rules);

        // Assert
        field.Status.Should().Be(FieldStatusType.HeatAlert);
        field.Alerts.Should().ContainSingle(a => a.AlertType == AlertType.ExtremeHeat && a.Status == AlertStatus.Active);

        SensorReading recoveryReading = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow.AddHours(1), airTemperature: 32.0);
        field.ProcessReading(recoveryReading, rules);

        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().ContainSingle(a => a.AlertType == AlertType.ExtremeHeat && a.Status == AlertStatus.Resolved);
    }

    [Fact]
    public void EvaluateExtremeHeatRule_WhenAirTemperatureMissing_ShouldNotChangeStatus()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        var rules = CreateRules(RuleType.ExtremeHeat, threshold: 40.0, windowHours: 4);
        SensorReading reading = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow, airTemperature: null);

        // Act
        field.ProcessReading(reading, rules);

        // Assert
        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateFrostRule_WhenConditionExceedsWindow_ShouldRaiseAndResolveAlert()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        var rules = CreateRules(RuleType.Frost, threshold: 2.0, windowHours: 2);

        SensorReading baseline = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow.AddHours(-3), airTemperature: 6.0);
        field.ProcessReading(baseline, rules);

        SensorReading frostReading = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow, airTemperature: -1.0);

        // Act
        field.ProcessReading(frostReading, rules);

        // Assert
        field.Status.Should().Be(FieldStatusType.FrostAlert);
        field.Alerts.Should().ContainSingle(a => a.AlertType == AlertType.Frost && a.Status == AlertStatus.Active);

        SensorReading recoveryReading = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow.AddHours(1), airTemperature: 7.0);
        field.ProcessReading(recoveryReading, rules);

        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().ContainSingle(a => a.AlertType == AlertType.Frost && a.Status == AlertStatus.Resolved);
    }

    [Fact]
    public void EvaluateDryAirRule_WhenConditionExceedsWindow_ShouldRaiseAndResolveAlert()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        var rules = CreateRules(RuleType.DryAir, threshold: 20.0, windowHours: 6);

        SensorReading baseline = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow.AddHours(-7), airHumidity: 40.0);
        field.ProcessReading(baseline, rules);

        SensorReading dryAirReading = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow, airHumidity: 10.0);

        // Act
        field.ProcessReading(dryAirReading, rules);

        // Assert
        field.Status.Should().Be(FieldStatusType.DryAirAlert);
        field.Alerts.Should().ContainSingle(a => a.AlertType == AlertType.DryAir && a.Status == AlertStatus.Active);

        SensorReading recoveryReading = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow.AddHours(1), airHumidity: 35.0);
        field.ProcessReading(recoveryReading, rules);

        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().ContainSingle(a => a.AlertType == AlertType.DryAir && a.Status == AlertStatus.Resolved);
    }

    [Fact]
    public void EvaluateDryAirRule_WhenAirHumidityMissing_ShouldNotChangeStatus()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        var rules = CreateRules(RuleType.DryAir, threshold: 20.0, windowHours: 6);
        SensorReading reading = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow, airHumidity: null);

        // Act
        field.ProcessReading(reading, rules);

        // Assert
        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateHumidAirRule_WhenConditionExceedsWindow_ShouldRaiseAndResolveAlert()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        var rules = CreateRules(RuleType.HumidAir, threshold: 90.0, windowHours: 12);

        SensorReading baseline = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow.AddHours(-13), airHumidity: 70.0);
        field.ProcessReading(baseline, rules);

        SensorReading humidReading = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow, airHumidity: 95.0);

        // Act
        field.ProcessReading(humidReading, rules);

        // Assert
        field.Status.Should().Be(FieldStatusType.HumidAirAlert);
        field.Alerts.Should().ContainSingle(a => a.AlertType == AlertType.HumidAir && a.Status == AlertStatus.Active);

        SensorReading recoveryReading = CreateReading("field-1", timestamp: DateTimeOffset.UtcNow.AddHours(1), airHumidity: 80.0);
        field.ProcessReading(recoveryReading, rules);

        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().ContainSingle(a => a.AlertType == AlertType.HumidAir && a.Status == AlertStatus.Resolved);
    }

    private static SensorReading CreateReading(
        string fieldId,
        DateTimeOffset timestamp,
        double soilMoisture = 45.0,
        double soilTemperature = 25.0,
        double rain = 0.0,
        double? airTemperature = 25.0,
        double? airHumidity = 50.0,
        string farmId = "farm-1")
    {
        Result<SensorReading> result = SensorReading.Create(
            readingId: Guid.NewGuid().ToString(),
            sensorId: "sensor-1",
            fieldId: fieldId,
            farmId: farmId,
            timestamp: timestamp,
            soilMoisturePercent: soilMoisture,
            soilTemperatureC: soilTemperature,
            rainMm: rain,
            airTemperatureC: airTemperature,
            airHumidityPercent: airHumidity,
            source: ReadingSource.Http);

        return result.Value!;
    }

    private static IReadOnlyList<Rule> CreateRules(RuleType ruleType, double threshold, int windowHours)
    {
        return [Rule.Create(ruleType, threshold, windowHours)];
    }
}
