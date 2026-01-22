using FieldMonitoring.Application.Alerts;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;

namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Adapter para gravar eventos de alertas no InfluxDB.
/// </summary>
public sealed class InfluxAlertEventsAdapter : IAlertEventsStore, IDisposable
{
    private const string TagFieldId = "fieldId";
    private const string TagFarmId = "farmId";
    private const string TagAlertType = "alertType";
    private const string FieldAlertId = "alertId";
    private const string FieldStatus = "status";
    private const string FieldReason = "reason";
    private const string FieldSeverity = "severity";

    private readonly InfluxDbOptions _options;
    private readonly InfluxDBClient _client;
    private readonly ILogger<InfluxAlertEventsAdapter> _logger;

    public InfluxAlertEventsAdapter(InfluxDbOptions options, ILogger<InfluxAlertEventsAdapter> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        if (!_options.IsConfigured())
        {
            throw new InvalidOperationException("InfluxDb enabled but missing configuration.");
        }

        _client = new InfluxDBClient(_options.Url!, _options.Token!);
    }

    public async Task AppendAsync(AlertEvent alertEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            PointData point = BuildPoint(alertEvent);
            var writeApi = _client.GetWriteApiAsync();
            await writeApi.WritePointAsync(point, _options.Bucket!, _options.Org!, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to write alert event {AlertId} for field {FieldId} to InfluxDb bucket {Bucket}",
                alertEvent.AlertId,
                alertEvent.FieldId,
                _options.Bucket);
            throw;
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private PointData BuildPoint(AlertEvent alertEvent)
    {
        PointData point = PointData
            .Measurement(_options.AlertMeasurement!)
            .Tag(TagFieldId, alertEvent.FieldId)
            .Tag(TagFarmId, alertEvent.FarmId)
            .Tag(TagAlertType, alertEvent.AlertType.ToString())
            .Field(FieldAlertId, alertEvent.AlertId.ToString())
            .Field(FieldStatus, alertEvent.Status.ToString())
            .Timestamp(NormalizeTimestamp(alertEvent.OccurredAt), WritePrecision.Ns);

        if (!string.IsNullOrWhiteSpace(alertEvent.Reason))
        {
            point = point.Field(FieldReason, alertEvent.Reason);
        }

        if (alertEvent.Severity.HasValue)
        {
            point = point.Field(FieldSeverity, alertEvent.Severity.Value);
        }

        return point;
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
}
