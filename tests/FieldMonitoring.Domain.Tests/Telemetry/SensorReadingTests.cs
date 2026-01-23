using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Domain.Tests.Telemetry;

public class SensorReadingTests
{
    [Fact]
    public void Create_WhenAllFieldsAreValid_ShouldReturnSuccess()
    {
        // Act
        Result<SensorReading> result = SensorReading.Create(
            readingId: "reading-1",
            sensorId: "sensor-1",
            fieldId: "field-1",
            farmId: "farm-1",
            timestamp: DateTimeOffset.UtcNow,
            soilMoisturePercent: 45.0,
            soilTemperatureC: 25.0,
            rainMm: 2.5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SoilMoisture.Percent.Should().Be(45.0);
        result.Value!.SoilTemperature.Celsius.Should().Be(25.0);
        result.Value!.Rain.Millimeters.Should().Be(2.5);
    }

    [Fact]
    public void Create_WhenReadingIdIsEmpty_ShouldReturnFailure()
    {
        // Act
        Result<SensorReading> result = SensorReading.Create(
            readingId: "",
            sensorId: "sensor-1",
            fieldId: "field-1",
            farmId: "farm-1",
            timestamp: DateTimeOffset.UtcNow,
            soilMoisturePercent: 45.0,
            soilTemperatureC: 25.0,
            rainMm: 2.5);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("ReadingId");
    }

    [Fact]
    public void Create_WhenSoilMoistureInvalid_ShouldReturnFailure()
    {
        // Act
        Result<SensorReading> result = SensorReading.Create(
            readingId: "reading-1",
            sensorId: "sensor-1",
            fieldId: "field-1",
            farmId: "farm-1",
            timestamp: DateTimeOffset.UtcNow,
            soilMoisturePercent: 150.0, // Invalid
            soilTemperatureC: 25.0,
            rainMm: 2.5);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Umidade do solo");
    }

    [Fact]
    public void Create_WhenTemperatureInvalid_ShouldReturnFailure()
    {
        // Act
        Result<SensorReading> result = SensorReading.Create(
            readingId: "reading-1",
            sensorId: "sensor-1",
            fieldId: "field-1",
            farmId: "farm-1",
            timestamp: DateTimeOffset.UtcNow,
            soilMoisturePercent: 45.0,
            soilTemperatureC: 100.0, // Invalid
            rainMm: 2.5);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Temperatura");
    }

    [Fact]
    public void Create_WhenRainNegative_ShouldReturnFailure()
    {
        // Act
        Result<SensorReading> result = SensorReading.Create(
            readingId: "reading-1",
            sensorId: "sensor-1",
            fieldId: "field-1",
            farmId: "farm-1",
            timestamp: DateTimeOffset.UtcNow,
            soilMoisturePercent: 45.0,
            soilTemperatureC: 25.0,
            rainMm: -1.0); // Invalid

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("chuva");
    }

    [Fact]
    public void Create_WithOptionalAirMetrics_ShouldReturnSuccess()
    {
        // Act
        Result<SensorReading> result = SensorReading.Create(
            readingId: "reading-1",
            sensorId: "sensor-1",
            fieldId: "field-1",
            farmId: "farm-1",
            timestamp: DateTimeOffset.UtcNow,
            soilMoisturePercent: 45.0,
            soilTemperatureC: 25.0,
            rainMm: 2.5,
            airTemperatureC: 28.0,
            airHumidityPercent: 65.0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AirTemperature.Should().NotBeNull();
        result.Value!.AirTemperature!.Celsius.Should().Be(28.0);
        result.Value!.AirHumidity.Should().NotBeNull();
        result.Value!.AirHumidity!.Percent.Should().Be(65.0);
    }
}
