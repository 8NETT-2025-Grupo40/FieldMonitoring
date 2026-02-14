using InfluxDB.Client;

namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Probe técnico para validar acesso ao bucket configurado no InfluxDB.
/// Usado pelos health checks de readiness.
/// </summary>
public sealed class InfluxBucketProbe : IInfluxBucketProbe
{
    private readonly InfluxDbOptions _options;
    private readonly IInfluxDBClient _client;

    public InfluxBucketProbe(InfluxDbOptions options, IInfluxDBClient client)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _client = client ?? throw new ArgumentNullException(nameof(client));

        if (!_options.IsConfigured())
        {
            throw new InvalidOperationException("InfluxDB habilitado, mas com configuração incompleta.");
        }
    }

    /// <summary>
    /// Verifica se o bucket configurado está acessível para o token atual.
    /// </summary>
    public async Task<bool> CanAccessConfiguredBucketAsync(CancellationToken cancellationToken = default)
    {
        var bucketsApi = _client.GetBucketsApi();
        var bucket = await bucketsApi.FindBucketByNameAsync(_options.Bucket!, cancellationToken);

        return bucket is not null;
    }
}
