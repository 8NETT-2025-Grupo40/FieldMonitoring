using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain;
using FieldMonitoring.Domain.Fields;
using FieldMonitoring.Domain.Rules;
using FieldMonitoring.Domain.Telemetry;
using Microsoft.Extensions.Logging;

namespace FieldMonitoring.Application.Tests.Telemetry;

public class ProcessTelemetryReadingUseCaseTests
{
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ITimeSeriesReadingsStore _timeSeriesStore;
    private readonly IAlertEventsStore _alertEventsStore;
    private readonly IFieldRepository _fieldRepository;
    private readonly IRuleSetProvider _ruleSetProvider;
    private readonly ILogger<ProcessTelemetryReadingUseCase> _logger;
    private readonly ProcessTelemetryReadingUseCase _useCase;

    public ProcessTelemetryReadingUseCaseTests()
    {
        _idempotencyStore = Substitute.For<IIdempotencyStore>();
        _timeSeriesStore = Substitute.For<ITimeSeriesReadingsStore>();
        _alertEventsStore = Substitute.For<IAlertEventsStore>();
        _fieldRepository = Substitute.For<IFieldRepository>();
        _ruleSetProvider = new DefaultRuleSetProvider();
        _logger = Substitute.For<ILogger<ProcessTelemetryReadingUseCase>>();

        _useCase = new ProcessTelemetryReadingUseCase(
            _idempotencyStore,
            _timeSeriesStore,
            _alertEventsStore,
            _fieldRepository,
            _ruleSetProvider,
            _logger);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadingAlreadyProcessed_ShouldSkip()
    {
        // Arrange
        TelemetryReceivedMessage message = CreateValidMessage();
        _idempotencyStore.ExistsAsync(message.ReadingId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        ProcessingResult result = await _useCase.ExecuteAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.WasSkipped.Should().BeTrue();
        result.ShouldRetry.Should().BeFalse();
        await _timeSeriesStore.DidNotReceive().AppendAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNewReading_ShouldProcessSuccessfully()
    {
        // Arrange
        TelemetryReceivedMessage message = CreateValidMessage();
        _idempotencyStore.ExistsAsync(message.ReadingId, Arg.Any<CancellationToken>())
            .Returns(false);
        _fieldRepository.GetByIdAsync(message.FieldId, Arg.Any<CancellationToken>())
            .Returns((Field?)null);

        // Act
        ProcessingResult result = await _useCase.ExecuteAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.WasSkipped.Should().BeFalse();
        result.ShouldRetry.Should().BeFalse();
        await _timeSeriesStore.Received(1).AppendAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>());
        await _fieldRepository.Received(1).SaveAsync(Arg.Any<Field>(), Arg.Any<CancellationToken>());
        await _idempotencyStore.Received(1).MarkProcessedAsync(Arg.Any<ProcessedReading>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidReading_ShouldReturnNonRetryableFailure()
    {
        // Arrange
        TelemetryReceivedMessage message = new TelemetryReceivedMessage
        {
            ReadingId = "", // Invalid - will throw exception
            SensorId = "sensor-1",
            FieldId = "field-1",
            FarmId = "farm-1",
            Timestamp = DateTimeOffset.UtcNow,
            SoilHumidity = 45.0,
            SoilTemperature = 25.0,
            RainMm = 2.5
        };

        // Act
        ProcessingResult result = await _useCase.ExecuteAsync(message);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ShouldRetry.Should().BeFalse();
        await _timeSeriesStore.DidNotReceive().AppendAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnexpectedError_ShouldReturnRetryableFailure()
    {
        // Arrange
        TelemetryReceivedMessage message = CreateValidMessage();
        _idempotencyStore.ExistsAsync(message.ReadingId, Arg.Any<CancellationToken>())
            .Returns(false);
        _timeSeriesStore.AppendAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("falha transitória"));

        // Act
        ProcessingResult result = await _useCase.ExecuteAsync(message);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ShouldRetry.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadingIsOutOfOrder_ShouldSkipWithoutSavingFieldState()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        TelemetryReceivedMessage message = new TelemetryReceivedMessage
        {
            ReadingId = Guid.NewGuid().ToString(),
            SensorId = "sensor-1",
            FieldId = "field-1",
            FarmId = "farm-1",
            Timestamp = now.AddHours(-1),
            SoilHumidity = 25.0,
            SoilTemperature = 25.0,
            RainMm = 1.0,
            Source = "http"
        };

        Field field = Field.Create("field-1", "farm-1");
        SensorReading baselineReading = CreateSensorReading(
            fieldId: "field-1",
            farmId: "farm-1",
            timestamp: now,
            soilMoisturePercent: 45.0);
        field.ProcessReading(baselineReading, [Rule.CreateDefaultDrynessRule()]);

        _idempotencyStore.ExistsAsync(message.ReadingId, Arg.Any<CancellationToken>())
            .Returns(false);
        _fieldRepository.GetByIdAsync(message.FieldId, Arg.Any<CancellationToken>())
            .Returns(field);

        // Act
        ProcessingResult result = await _useCase.ExecuteAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.WasSkipped.Should().BeTrue();
        result.ShouldRetry.Should().BeFalse();
        await _fieldRepository.DidNotReceive().SaveAsync(Arg.Any<Field>(), Arg.Any<CancellationToken>());
        await _idempotencyStore.Received(1).MarkProcessedAsync(Arg.Any<ProcessedReading>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadingFarmDiffersFromFieldFarm_ShouldReturnNonRetryableFailure()
    {
        // Arrange
        TelemetryReceivedMessage message = new TelemetryReceivedMessage
        {
            ReadingId = Guid.NewGuid().ToString(),
            SensorId = "sensor-1",
            FieldId = "field-1",
            FarmId = "farm-2",
            Timestamp = DateTimeOffset.UtcNow,
            SoilHumidity = 45.0,
            SoilTemperature = 25.0,
            RainMm = 2.5,
            Source = "http"
        };

        _idempotencyStore.ExistsAsync(message.ReadingId, Arg.Any<CancellationToken>())
            .Returns(false);
        _fieldRepository.GetByIdAsync(message.FieldId, Arg.Any<CancellationToken>())
            .Returns(Field.Create("field-1", "farm-1"));

        // Act
        ProcessingResult result = await _useCase.ExecuteAsync(message);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ShouldRetry.Should().BeFalse();
        await _fieldRepository.DidNotReceive().SaveAsync(Arg.Any<Field>(), Arg.Any<CancellationToken>());
        await _idempotencyStore.DidNotReceive().MarkProcessedAsync(Arg.Any<ProcessedReading>(), Arg.Any<CancellationToken>());
    }

    private static TelemetryReceivedMessage CreateValidMessage()
    {
        return new TelemetryReceivedMessage
        {
            ReadingId = Guid.NewGuid().ToString(),
            SensorId = "sensor-1",
            FieldId = "field-1",
            FarmId = "farm-1",
            Timestamp = DateTimeOffset.UtcNow,
            SoilHumidity = 45.0,
            SoilTemperature = 25.0,
            RainMm = 2.5,
            Source = "http"
        };
    }

    private static SensorReading CreateSensorReading(
        string fieldId,
        string farmId,
        DateTimeOffset timestamp,
        double soilMoisturePercent)
    {
        Result<SensorReading> result = SensorReading.Create(
            readingId: Guid.NewGuid().ToString(),
            sensorId: "sensor-1",
            fieldId: fieldId,
            farmId: farmId,
            timestamp: timestamp,
            soilMoisturePercent: soilMoisturePercent,
            soilTemperatureC: 25.0,
            rainMm: 0.0,
            source: ReadingSource.Http);

        return result.Value!;
    }
}
