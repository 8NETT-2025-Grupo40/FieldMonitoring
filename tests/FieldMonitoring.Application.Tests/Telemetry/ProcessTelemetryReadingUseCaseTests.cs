using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain.Fields;
using FieldMonitoring.Domain.Telemetry;
using Microsoft.Extensions.Logging;

namespace FieldMonitoring.Application.Tests.Telemetry;

public class ProcessTelemetryReadingUseCaseTests
{
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ITimeSeriesReadingsStore _timeSeriesStore;
    private readonly IAlertEventsStore _alertEventsStore;
    private readonly IFieldRepository _fieldRepository;
    private readonly ILogger<ProcessTelemetryReadingUseCase> _logger;
    private readonly ProcessTelemetryReadingUseCase _useCase;

    public ProcessTelemetryReadingUseCaseTests()
    {
        _idempotencyStore = Substitute.For<IIdempotencyStore>();
        _timeSeriesStore = Substitute.For<ITimeSeriesReadingsStore>();
        _alertEventsStore = Substitute.For<IAlertEventsStore>();
        _fieldRepository = Substitute.For<IFieldRepository>();
        _logger = Substitute.For<ILogger<ProcessTelemetryReadingUseCase>>();

        _useCase = new ProcessTelemetryReadingUseCase(
            _idempotencyStore,
            _timeSeriesStore,
            _alertEventsStore,
            _fieldRepository,
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
        await _timeSeriesStore.Received(1).AppendAsync(Arg.Any<SensorReading>(), Arg.Any<CancellationToken>());
        await _fieldRepository.Received(1).SaveAsync(Arg.Any<Field>(), Arg.Any<CancellationToken>());
        await _idempotencyStore.Received(1).MarkProcessedAsync(Arg.Any<ProcessedReading>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidReading_ShouldLogError()
    {
        // Arrange
        TelemetryReceivedMessage message = new TelemetryReceivedMessage
        {
            ReadingId = "", // Invalid - will throw exception
            SensorId = "sensor-1",
            FieldId = "field-1",
            FarmId = "farm-1",
            Timestamp = DateTime.UtcNow,
            SoilHumidity = 45.0,
            SoilTemperature = 25.0,
            RainMm = 2.5
        };

        // Act
        ProcessingResult result = await _useCase.ExecuteAsync(message);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static TelemetryReceivedMessage CreateValidMessage()
    {
        return new TelemetryReceivedMessage
        {
            ReadingId = Guid.NewGuid().ToString(),
            SensorId = "sensor-1",
            FieldId = "field-1",
            FarmId = "farm-1",
            Timestamp = DateTime.UtcNow,
            SoilHumidity = 45.0,
            SoilTemperature = 25.0,
            RainMm = 2.5,
            Source = "http"
        };
    }
}
