using FieldMonitoring.Application.Alerts;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Adapter para gravar eventos de alertas no InfluxDB.
/// </summary>
public sealed class InfluxAlertEventsAdapter : IAlertEventsStore
{
    private const string TagFieldId = "fieldId";
    private const string TagFarmId = "farmId";
    private const string TagAlertType = "alertType";
    private const string FieldAlertId = "alertId";
    private const string FieldStatus = "status";
    private const string FieldActive = "active";
    private const string FieldReason = "reason";
    private const string FieldSeverity = "severity";

    private readonly InfluxDbOptions _options;
    private readonly IInfluxDBClient _client;

    public InfluxAlertEventsAdapter(InfluxDbOptions options, IInfluxDBClient client)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _client = client ?? throw new ArgumentNullException(nameof(client));

        if (!_options.IsConfigured())
        {
            throw new InvalidOperationException("InfluxDB habilitado, mas com configuração incompleta.");
        }
    }

    public async Task AppendAsync(AlertEvent alertEvent, CancellationToken cancellationToken = default)
    {
        PointData point = BuildPoint(alertEvent);
        var writeApi = _client.GetWriteApiAsync();
        await writeApi.WritePointAsync(point, _options.Bucket!, _options.Org!, cancellationToken);
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
            .Field(FieldActive, alertEvent.Status == Domain.Alerts.AlertStatus.Active ? 1 : 0)
            .Timestamp(InfluxTimestampHelper.NormalizeToUtc(alertEvent.OccurredAt), WritePrecision.Ns);

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
}
