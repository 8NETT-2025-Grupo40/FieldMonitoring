using Microsoft.Extensions.Configuration;

namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

public sealed class InfluxDbOptions
{
    public const string SectionName = "InfluxDb";

    public string? Url { get; set; }
    public string? Token { get; set; }
    public string? Org { get; set; }
    public string? Bucket { get; set; }
    public string? Measurement { get; set; }
    public string? AlertMeasurement { get; set; }
    public bool Enabled { get; set; }

    public static InfluxDbOptions Load(IConfiguration configuration)
    {
        var options = new InfluxDbOptions();
        configuration.GetSection(SectionName).Bind(options);

        options.Url = PreferConfigured(options.Url, configuration["INFLUXDB_URL"]);
        options.Token = PreferConfigured(options.Token, configuration["INFLUXDB_TOKEN"]);
        options.Org = PreferConfigured(options.Org, configuration["INFLUXDB_ORG"]);
        options.Bucket = PreferConfigured(options.Bucket, configuration["INFLUXDB_BUCKET"]);
        options.Measurement = PreferConfigured(options.Measurement, configuration["INFLUXDB_MEASUREMENT"]) ?? "telemetry_readings";
        options.AlertMeasurement = PreferConfigured(options.AlertMeasurement, configuration["INFLUXDB_ALERT_MEASUREMENT"]) ?? "field_alerts";

        string? enabledValue = configuration["INFLUXDB_ENABLED"];
        if (!string.IsNullOrWhiteSpace(enabledValue) && bool.TryParse(enabledValue, out bool enabled))
        {
            options.Enabled = enabled;
        }

        return options;
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(Url)
            && !string.IsNullOrWhiteSpace(Token)
            && !string.IsNullOrWhiteSpace(Org)
            && !string.IsNullOrWhiteSpace(Bucket)
            && !string.IsNullOrWhiteSpace(Measurement)
            && !string.IsNullOrWhiteSpace(AlertMeasurement);
    }

    private static string? PreferConfigured(string? configured, string? fallback)
    {
        return string.IsNullOrWhiteSpace(configured) ? fallback : configured;
    }
}
