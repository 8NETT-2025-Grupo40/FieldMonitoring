using Microsoft.Extensions.Configuration;

namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Opções de configuração para conexão com o InfluxDB.
/// Carregadas da seção "InfluxDb" do appsettings ou de variáveis de ambiente com prefixo INFLUXDB_.
/// </summary>
public sealed class InfluxDbOptions
{
    /// <summary>Nome da seção no arquivo de configuração.</summary>
    public const string SectionName = "InfluxDb";

    /// <summary>URL de conexão com o InfluxDB (ex: http://localhost:8086).</summary>
    public string? Url { get; set; }

    /// <summary>Token de autenticação para o InfluxDB.</summary>
    public string? Token { get; set; }

    /// <summary>Nome da organização no InfluxDB.</summary>
    public string? Org { get; set; }

    /// <summary>Nome do bucket para armazenamento de dados.</summary>
    public string? Bucket { get; set; }

    /// <summary>Nome do measurement para leituras de telemetria.</summary>
    public string? Measurement { get; set; }

    /// <summary>Nome do measurement para eventos de alertas.</summary>
    public string? AlertMeasurement { get; set; }

    /// <summary>Indica se a integração com InfluxDB está habilitada.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Carrega as opções da configuração, priorizando valores da seção InfluxDb
    /// e usando variáveis de ambiente como fallback.
    /// </summary>
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

    /// <summary>
    /// Verifica se todas as propriedades obrigatórias estão preenchidas.
    /// </summary>
    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(Url)
            && !string.IsNullOrWhiteSpace(Token)
            && !string.IsNullOrWhiteSpace(Org)
            && !string.IsNullOrWhiteSpace(Bucket)
            && !string.IsNullOrWhiteSpace(Measurement)
            && !string.IsNullOrWhiteSpace(AlertMeasurement);
    }

    /// <summary>
    /// Retorna o valor configurado se preenchido, caso contrário retorna o fallback.
    /// </summary>
    private static string? PreferConfigured(string? configured, string? fallback)
    {
        return string.IsNullOrWhiteSpace(configured) ? fallback : configured;
    }
}
