namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Contrato para validar acesso ao bucket configurado no InfluxDB.
/// </summary>
public interface IInfluxBucketProbe
{
    /// <summary>
    /// Retorna <c>true</c> quando o bucket configurado está acessível para o token atual.
    /// </summary>
    Task<bool> CanAccessConfiguredBucketAsync(CancellationToken cancellationToken = default);
}
