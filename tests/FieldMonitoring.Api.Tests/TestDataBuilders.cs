using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain.Rules;

namespace FieldMonitoring.Api.Tests;

/// <summary>
/// Builder para criar mensagens de telemetria para testes.
/// </summary>
public class TelemetryMessageBuilder
{
    private string _readingId = Guid.NewGuid().ToString();
    private string _sensorId = "sensor-1";
    private string _fieldId = "field-1";
    private string _farmId = "farm-1";
    private DateTimeOffset _timestamp = DateTimeOffset.UtcNow;
    private double _soilHumidity = 45.0;
    private double _soilTemperature = 25.0;
    private double? _airTemperature = null;
    private double? _airHumidity = null;
    private double _rainMm = 2.5;
    private string _source = "http";

    public TelemetryMessageBuilder WithReadingId(string readingId)
    {
        _readingId = readingId;
        return this;
    }

    public TelemetryMessageBuilder WithSensorId(string sensorId)
    {
        _sensorId = sensorId;
        return this;
    }

    public TelemetryMessageBuilder ForField(string fieldId, string farmId)
    {
        _fieldId = fieldId;
        _farmId = farmId;
        return this;
    }

    public TelemetryMessageBuilder WithSoilMoisture(double percent)
    {
        _soilHumidity = percent;
        return this;
    }

    public TelemetryMessageBuilder WithTemperature(double celsius)
    {
        _soilTemperature = celsius;
        return this;
    }

    public TelemetryMessageBuilder WithAirTemperature(double celsius)
    {
        _airTemperature = celsius;
        return this;
    }

    public TelemetryMessageBuilder WithAirHumidity(double percent)
    {
        _airHumidity = percent;
        return this;
    }

    public TelemetryMessageBuilder WithRain(double millimeters)
    {
        _rainMm = millimeters;
        return this;
    }

    public TelemetryMessageBuilder WithTimestamp(DateTimeOffset timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    public TelemetryMessageBuilder WithSource(string source)
    {
        _source = source;
        return this;
    }

    public TelemetryReceivedMessage Build()
    {
        return new TelemetryReceivedMessage
        {
            ReadingId = _readingId,
            SensorId = _sensorId,
            FieldId = _fieldId,
            FarmId = _farmId,
            Timestamp = _timestamp,
            SoilHumidity = _soilHumidity,
            SoilTemperature = _soilTemperature,
            AirTemperature = _airTemperature,
            AirHumidity = _airHumidity,
            RainMm = _rainMm,
            Source = _source
        };
    }
}

/// <summary>
/// Builder para criar regras de seca para testes.
/// </summary>
public class RuleBuilder
{
    private Guid _ruleId = Guid.NewGuid();
    private RuleType _ruleType = RuleType.Dryness;
    private bool _isEnabled = true;
    private double _threshold = 30.0;
    private int _windowHours = 24;

    public RuleBuilder WithThreshold(double threshold)
    {
        _threshold = threshold;
        return this;
    }

    public RuleBuilder WithWindowHours(int hours)
    {
        _windowHours = hours;
        return this;
    }

    public RuleBuilder Disabled()
    {
        _isEnabled = false;
        return this;
    }

    public Rule Build()
    {
        return new Rule
        {
            RuleId = _ruleId,
            RuleType = _ruleType,
            IsEnabled = _isEnabled,
            Threshold = _threshold,
            WindowHours = _windowHours
        };
    }
}
