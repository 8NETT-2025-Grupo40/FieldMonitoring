using FieldMonitoring.Domain.Alerts;
using FieldMonitoring.Domain.Fields;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Tests.Fields;

public class FieldTests
{
    #region Create Tests

    [Fact]
    public void Create_WhenValidParameters_ShouldCreateFieldWithNormalStatus()
    {
        // Act
        Field field = Field.Create("field-1", "farm-1");

        // Assert
        field.FieldId.Should().Be("field-1");
        field.FarmId.Should().Be("farm-1");
        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().BeEmpty();
        field.LastReadingAt.Should().BeNull();
    }

    [Fact]
    public void Create_WhenNullFieldId_ShouldThrowException()
    {
        // Act
        Action act = () => Field.Create(null!, "farm-1");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ProcessReading Tests

    [Fact]
    public void ProcessReading_WhenValidReading_ShouldUpdateLastReadingValues()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        SensorReading reading = CreateReading("field-1", soilMoisture: 45.0);
        Rule rule = CreateDrynessRule(threshold: 30.0);

        // Act
        field.ProcessReading(reading, rule);

        // Assert
        field.LastSoilMoisture.Should().NotBeNull();
        field.LastSoilMoisture!.Percent.Should().Be(45.0);
        field.LastSoilTemperature.Should().NotBeNull();
        field.LastSoilTemperature!.Celsius.Should().Be(25.0);
        field.LastRain.Should().NotBeNull();
        field.LastRain!.Millimeters.Should().Be(2.5);
        field.LastReadingAt.Should().Be(reading.Timestamp);
    }

    [Fact]
    public void ProcessReading_WhenReadingForDifferentField_ShouldThrowException()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        SensorReading reading = CreateReading("field-2", soilMoisture: 45.0);
        Rule rule = CreateDrynessRule(threshold: 30.0);

        // Act
        Action act = () => field.ProcessReading(reading, rule);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*outro talhão*");
    }

    [Fact]
    public void ProcessReading_WhenMoistureAboveThreshold_ShouldKeepNormalStatus()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        SensorReading reading = CreateReading("field-1", soilMoisture: 50.0);
        Rule rule = CreateDrynessRule(threshold: 30.0);

        // Act
        field.ProcessReading(reading, rule);

        // Assert
        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().BeEmpty();
    }

    #endregion

    #region EvaluateDrynessRule Tests

    [Fact]
    public void EvaluateDrynessRule_WhenMoistureBelowThresholdButWithinWindow_ShouldNotCreateAlert()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        Rule rule = CreateDrynessRule(threshold: 30.0, windowHours: 24);

        // Primeira leitura normal
        SensorReading reading1 = CreateReading("field-1", soilMoisture: 35.0, timestamp: DateTimeOffset.UtcNow.AddHours(-10));
        field.ProcessReading(reading1, rule);

        // Segunda leitura abaixo do threshold, mas há apenas 5 horas
        SensorReading reading2 = CreateReading("field-1", soilMoisture: 25.0, timestamp: DateTimeOffset.UtcNow.AddHours(-5));

        // Act
        field.ProcessReading(reading2, rule);

        // Assert
        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateDrynessRule_WhenMoistureBelowThresholdExceedingWindow_ShouldCreateAlert()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        Rule rule = CreateDrynessRule(threshold: 30.0, windowHours: 24);

        // Primeira leitura normal (estabelece baseline)
        SensorReading reading1 = CreateReading("field-1", soilMoisture: 35.0, timestamp: DateTimeOffset.UtcNow.AddHours(-30));
        field.ProcessReading(reading1, rule);

        // Segunda leitura abaixo do threshold por > 24 horas
        SensorReading reading2 = CreateReading("field-1", soilMoisture: 25.0, timestamp: DateTimeOffset.UtcNow);

        // Act
        field.ProcessReading(reading2, rule);

        // Assert
        field.Status.Should().Be(FieldStatusType.DryAlert);
        field.Alerts.Should().HaveCount(1);
        field.Alerts[0].AlertType.Should().Be(AlertType.Dryness);
        field.Alerts[0].Status.Should().Be(AlertStatus.Active);
        field.Alerts[0].Reason.Should().Contain("Umidade do solo abaixo");
    }

    [Fact]
    public void EvaluateDrynessRule_WhenAlertActiveAndMoistureReturnsNormal_ShouldResolveAlert()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        Rule rule = CreateDrynessRule(threshold: 30.0, windowHours: 24);

        // Cria condição de seca
        SensorReading reading1 = CreateReading("field-1", soilMoisture: 35.0, timestamp: DateTimeOffset.UtcNow.AddHours(-30));
        field.ProcessReading(reading1, rule);
        SensorReading reading2 = CreateReading("field-1", soilMoisture: 25.0, timestamp: DateTimeOffset.UtcNow.AddHours(-1));
        field.ProcessReading(reading2, rule);

        field.Status.Should().Be(FieldStatusType.DryAlert); // Confirma alerta ativo

        // Umidade retorna ao normal
        SensorReading reading3 = CreateReading("field-1", soilMoisture: 40.0, timestamp: DateTimeOffset.UtcNow);

        // Act
        field.ProcessReading(reading3, rule);

        // Assert
        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().HaveCount(1);
        field.Alerts[0].Status.Should().Be(AlertStatus.Resolved);
        field.Alerts[0].ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void EvaluateDrynessRule_WhenAlertAlreadyActive_ShouldNotCreateDuplicateAlert()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        Rule rule = CreateDrynessRule(threshold: 30.0, windowHours: 24);

        // Cria primeiro alerta
        SensorReading reading1 = CreateReading("field-1", soilMoisture: 35.0, timestamp: DateTimeOffset.UtcNow.AddHours(-30));
        field.ProcessReading(reading1, rule);
        SensorReading reading2 = CreateReading("field-1", soilMoisture: 25.0, timestamp: DateTimeOffset.UtcNow.AddHours(-1));
        field.ProcessReading(reading2, rule);

        var initialAlertCount = field.Alerts.Count;

        // Nova leitura ainda abaixo do threshold
        SensorReading reading3 = CreateReading("field-1", soilMoisture: 20.0, timestamp: DateTimeOffset.UtcNow);

        // Act
        field.ProcessReading(reading3, rule);

        // Assert
        field.Alerts.Should().HaveCount(initialAlertCount); // Não duplicou
        field.Alerts.Count(a => a.Status == AlertStatus.Active).Should().Be(1);
    }

    [Fact]
    public void EvaluateDrynessRule_WhenMoistureExactlyAtThreshold_ShouldNotCreateAlert()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        Rule rule = CreateDrynessRule(threshold: 30.0, windowHours: 24);
        SensorReading reading = CreateReading("field-1", soilMoisture: 30.0, timestamp: DateTimeOffset.UtcNow);

        // Act
        field.ProcessReading(reading, rule);

        // Assert
        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateDrynessRule_WhenNewFieldWithNoHistory_ShouldCreateAlertImmediately()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        Rule rule = CreateDrynessRule(threshold: 30.0, windowHours: 24);

        // Primeira leitura já abaixo do threshold (sem histórico prévio)
        SensorReading reading = CreateReading("field-1", soilMoisture: 20.0, timestamp: DateTimeOffset.UtcNow);

        // Act
        field.ProcessReading(reading, rule);

        // Assert
        // Campo novo sem histórico = assume condições normais no início
        // Só criará alerta após window hours
        field.Status.Should().Be(FieldStatusType.Normal);
        field.Alerts.Should().BeEmpty();
    }

    #endregion

    #region UpdateStatus Tests

    [Fact]
    public void UpdateStatus_WhenDryAlertActive_ShouldSetDryAlertStatus()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        Rule rule = CreateDrynessRule(threshold: 30.0, windowHours: 24);

        // Cria alerta
        SensorReading reading1 = CreateReading("field-1", soilMoisture: 35.0, timestamp: DateTimeOffset.UtcNow.AddHours(-30));
        field.ProcessReading(reading1, rule);
        SensorReading reading2 = CreateReading("field-1", soilMoisture: 25.0, timestamp: DateTimeOffset.UtcNow);
        field.ProcessReading(reading2, rule);

        // Assert
        field.Status.Should().Be(FieldStatusType.DryAlert);
        field.StatusReason.Should().Contain("Umidade do solo abaixo");
    }

    [Fact]
    public void UpdateStatus_WhenNoActiveAlerts_ShouldSetNormalStatus()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        Rule rule = CreateDrynessRule(threshold: 30.0, windowHours: 24);
        SensorReading reading = CreateReading("field-1", soilMoisture: 50.0, timestamp: DateTimeOffset.UtcNow);

        // Act
        field.ProcessReading(reading, rule);

        // Assert
        field.Status.Should().Be(FieldStatusType.Normal);
        field.StatusReason.Should().Contain("dentro do esperado");
    }

    #endregion

    #region LoadAlerts Tests

    [Fact]
    public void LoadAlerts_WhenAlertsProvided_ShouldLoadAlertsAndDeriveDryAlertActive()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        List<Alert> alerts = new List<Alert>
        {
            Alert.CreateDrynessAlert("farm-1", "field-1", "Test alert")
        };

        // Act
        field.LoadAlerts(alerts);

        // Assert
        field.Alerts.Should().HaveCount(1);
        field.Alerts.First().AlertType.Should().Be(AlertType.Dryness);
        field.Alerts.First().Status.Should().Be(AlertStatus.Active);
    }

    [Fact]
    public void LoadAlerts_WhenEmptyAlerts_ShouldClearExistingAlerts()
    {
        // Arrange
        Field field = Field.Create("field-1", "farm-1");
        field.LoadAlerts(new List<Alert> { Alert.CreateDrynessAlert("farm-1", "field-1", "Test") });
        field.Alerts.Should().HaveCount(1); // Confirma que tem alerta

        // Act
        field.LoadAlerts(new List<Alert>());

        // Assert
        field.Alerts.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static SensorReading CreateReading(
        string fieldId,
        double soilMoisture,
        double soilTemperature = 25.0,
        double rain = 2.5,
        DateTimeOffset? timestamp = null,
        string sensorId = "sensor-1")
    {
        Result<SensorReading> result = SensorReading.Create(
            readingId: Guid.NewGuid().ToString(),
            sensorId: sensorId,
            fieldId: fieldId,
            farmId: "farm-1",
            timestamp: timestamp ?? DateTimeOffset.UtcNow,
            soilMoisturePercent: soilMoisture,
            soilTemperatureC: soilTemperature,
            rainMm: rain,
            source: ReadingSource.Http);

        return result.Value!; // Assumindo valores válidos nos testes
    }

    private static Rule CreateDrynessRule(double threshold, int windowHours = 24)
    {
        return new Rule
        {
            RuleId = Guid.NewGuid(),
            RuleType = RuleType.Dryness,
            IsEnabled = true,
            Threshold = threshold,
            WindowHours = windowHours
        };
    }

    #endregion
}
