using FieldMonitoring.Domain.Alerts;

namespace FieldMonitoring.Application.Alerts;

/// <summary>
/// Query para obter um alerta pelo seu identificador.
/// </summary>
public class GetAlertByIdQuery
{
    private readonly IAlertStore _alertStore;

    public GetAlertByIdQuery(IAlertStore alertStore)
    {
        _alertStore = alertStore;
    }

    /// <summary>
    /// Executa a query para obter um alerta pelo ID.
    /// Retorna <c>null</c> se o alerta não existir.
    /// </summary>
    public async Task<AlertDto?> ExecuteAsync(
        Guid alertId,
        CancellationToken cancellationToken = default)
    {
        Alert? alert = await _alertStore.GetByIdAsync(alertId, cancellationToken);
        return alert is null ? null : AlertDto.FromEntity(alert);
    }
}
