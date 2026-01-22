using System.Globalization;
using System.Text;
using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain.Telemetry;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Adapter de series temporais usando InfluxDB.
/// Influx organiza dados por measurement (serie), tags (filtros) e fields (valores).
/// </summary>
public sealed class InfluxTimeSeriesAdapter : ITimeSeriesReadingsStore, IDisposable
{
    private const string TagFieldId = "fieldId";
    private const string TagFarmId = "farmId";
    private const string TagSensorId = "sensorId";
    private const string TagSource = "source";
    private const string FieldReadingId = "readingId";
    private const string FieldSoilHumidity = "soilHumidity";
    private const string FieldSoilTemperature = "soilTemperature";
    private const string FieldRainMm = "rainMm";
    private const string FieldAirTemperature = "airTemperature";
    private const string FieldAirHumidity = "airHumidity";
    private const string SourceHttp = "http";
    private const string SourceMqtt = "mqtt";

    private readonly InfluxDbOptions _options;
    private readonly InfluxDBClient _client;
    private readonly ILogger<InfluxTimeSeriesAdapter> _logger;

    public InfluxTimeSeriesAdapter(InfluxDbOptions options, ILogger<InfluxTimeSeriesAdapter> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        if (!_options.IsConfigured())
        {
            throw new InvalidOperationException("InfluxDb enabled but missing configuration.");
        }

        _client = new InfluxDBClient(_options.Url!, _options.Token!);
    }

    public async Task AppendAsync(SensorReading reading, CancellationToken cancellationToken = default)
    {
        try
        {
            PointData point = BuildPoint(reading);
            var writeApi = _client.GetWriteApiAsync();
            await writeApi.WritePointAsync(point, _options.Bucket!, _options.Org!, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to write reading {ReadingId} for field {FieldId} to InfluxDb bucket {Bucket}",
                reading.ReadingId,
                reading.FieldId,
                _options.Bucket);
            throw;
        }
    }

    public async Task<IReadOnlyList<SensorReading>> GetByPeriodAsync(
        string fieldId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        string flux = BuildQuery(fieldId, from, to);
        var queryApi = _client.GetQueryApi();
        List<FluxTable> tables = await queryApi.QueryAsync(flux, _options.Org!, cancellationToken);

        List<SensorReading> readings = new();
        foreach (FluxTable table in tables)
        {
            foreach (FluxRecord record in table.Records)
            {
                if (TryBuildReading(record, out SensorReading reading))
                {
                    readings.Add(reading);
                }
            }
        }

        return readings
            .OrderBy(r => r.Timestamp)
            .ToList();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private PointData BuildPoint(SensorReading reading)
    {
        // Tags sao filtros com cardinalidade controlada. Fields sao valores numericos.
        PointData point = PointData
            .Measurement(_options.Measurement!)
            .Tag(TagFieldId, reading.FieldId)
            .Tag(TagFarmId, reading.FarmId)
            .Tag(TagSensorId, reading.SensorId)
            .Tag(TagSource, reading.Source == ReadingSource.Mqtt ? SourceMqtt : SourceHttp)
            .Field(FieldReadingId, reading.ReadingId)
            .Field(FieldSoilHumidity, reading.SoilMoisture.Percent)
            .Field(FieldSoilTemperature, reading.SoilTemperature.Celsius)
            .Field(FieldRainMm, reading.Rain.Millimeters)
            .Timestamp(NormalizeTimestamp(reading.Timestamp), WritePrecision.Ns);

        if (reading.AirTemperature != null)
        {
            point = point.Field(FieldAirTemperature, reading.AirTemperature.Celsius);
        }

        if (reading.AirHumidity != null)
        {
            point = point.Field(FieldAirHumidity, reading.AirHumidity.Percent);
        }

        return point;
    }

    private string BuildQuery(string fieldId, DateTime from, DateTime to)
    {
        DateTime start = NormalizeTimestamp(from);
        DateTime stop = NormalizeTimestamp(to);
        if (start > stop)
        {
            (start, stop) = (stop, start);
        }

        if (start == stop)
        {
            stop = start.AddSeconds(1);
        }

        string bucket = EscapeFluxString(_options.Bucket!);
        string measurement = EscapeFluxString(_options.Measurement!);
        string safeFieldId = EscapeFluxString(fieldId);

        var builder = new StringBuilder();
        builder.Append("from(bucket: \"").Append(bucket).Append("\")\n");
        builder.Append("  |> range(start: ")
            .Append(start.ToString("O", CultureInfo.InvariantCulture))
            .Append(", stop: ")
            .Append(stop.ToString("O", CultureInfo.InvariantCulture))
            .Append(")\n");
        builder.Append("  |> filter(fn: (r) => r._measurement == \"").Append(measurement).Append("\")\n");
        builder.Append("  |> filter(fn: (r) => r.").Append(TagFieldId).Append(" == \"").Append(safeFieldId).Append("\")\n");
        // Pivot transforma linhas (_field/_value) em colunas para reconstruir a leitura.
        builder.Append("  |> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")\n");
        builder.Append("  |> sort(columns: [\"_time\"])");

        return builder.ToString();
    }

    private bool TryBuildReading(FluxRecord record, out SensorReading reading)
    {
        DateTime? timestamp = GetTimestamp(record);
        string? fieldId = GetStringValue(record, TagFieldId);
        string? farmId = GetStringValue(record, TagFarmId);
        string? sensorId = GetStringValue(record, TagSensorId);

        if (timestamp == null || string.IsNullOrWhiteSpace(fieldId) || string.IsNullOrWhiteSpace(farmId) || string.IsNullOrWhiteSpace(sensorId))
        {
            _logger.LogDebug("Skipping InfluxDb record: required tags are missing.");
            reading = null!;
            return false;
        }

        double? soilHumidity = GetDoubleValue(record, FieldSoilHumidity);
        double? soilTemperature = GetDoubleValue(record, FieldSoilTemperature);
        double? rainMm = GetDoubleValue(record, FieldRainMm);

        if (!soilHumidity.HasValue || !soilTemperature.HasValue || !rainMm.HasValue)
        {
            _logger.LogDebug("Skipping InfluxDb record: required fields are missing for field {FieldId}.", fieldId);
            reading = null!;
            return false;
        }

        double? airTemperature = GetDoubleValue(record, FieldAirTemperature);
        double? airHumidity = GetDoubleValue(record, FieldAirHumidity);

        string readingId = GetStringValue(record, FieldReadingId)
            ?? $"influx-{fieldId}-{timestamp:O}";

        ReadingSource source = ParseSource(GetStringValue(record, TagSource));

        Result<SensorReading> result = SensorReading.Create(
            readingId: readingId,
            sensorId: sensorId,
            fieldId: fieldId,
            farmId: farmId,
            timestamp: NormalizeTimestamp(timestamp.Value),
            soilMoisturePercent: soilHumidity.Value,
            soilTemperatureC: soilTemperature.Value,
            rainMm: rainMm.Value,
            airTemperatureC: airTemperature,
            airHumidityPercent: airHumidity,
            source: source);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Ignoring invalid reading from InfluxDb: {Error}", result.Error);
            reading = null!;
            return false;
        }

        reading = result.Value!;
        return true;
    }

    private static string EscapeFluxString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static DateTime? GetTimestamp(FluxRecord record)
    {
        if (!record.Values.TryGetValue("_time", out object? value) || value == null)
        {
            return null;
        }

        return value switch
        {
            DateTime dateTime => NormalizeTimestamp(dateTime),
            DateTimeOffset dateTimeOffset => dateTimeOffset.UtcDateTime,
            Instant instant => instant.ToDateTimeUtc(),
            string text when DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsed) => parsed,
            _ => null
        };
    }

    private static DateTime NormalizeTimestamp(DateTime timestamp)
    {
        if (timestamp.Kind == DateTimeKind.Utc)
        {
            return timestamp;
        }

        if (timestamp.Kind == DateTimeKind.Local)
        {
            return timestamp.ToUniversalTime();
        }

        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }

    private static double? GetDoubleValue(FluxRecord record, string key)
    {
        if (!record.Values.TryGetValue(key, out object? value) || value == null)
        {
            return null;
        }

        return value switch
        {
            double d => d,
            float f => f,
            decimal m => (double)m,
            long l => l,
            int i => i,
            short s => s,
            uint ui => ui,
            ulong ul => ul,
            string text when double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed) => parsed,
            _ => null
        };
    }

    private static string? GetStringValue(FluxRecord record, string key)
    {
        if (!record.Values.TryGetValue(key, out object? value) || value == null)
        {
            return null;
        }

        return value.ToString();
    }

    private static ReadingSource ParseSource(string? source)
    {
        return string.Equals(source, SourceMqtt, StringComparison.OrdinalIgnoreCase)
            ? ReadingSource.Mqtt
            : ReadingSource.Http;
    }

}
