using FieldMonitoring.Domain.Alerts;

namespace FieldMonitoring.Application.Alerts;

/// <summary>
/// Query para obter alertas ativos.
/// </summary>
public class GetActiveAlertsQuery
{
    private readonly IAlertStore _alertStore;

    public GetActiveAlertsQuery(IAlertStore alertStore)
    {
        _alertStore = alertStore;
    }

    /// <summary>
    /// Obtém todos os alertas ativos de uma fazenda.
    /// </summary>
    public async Task<IReadOnlyList<AlertDto>> ExecuteByFarmAsync(
        string farmId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Alert> alerts = await _alertStore.GetActiveByFarmAsync(farmId, cancellationToken);
        return alerts.Select(AlertDto.FromEntity).ToList();
    }

    /// <summary>
    /// Obtém todos os alertas ativos de um talhão.
    /// </summary>
    public async Task<IReadOnlyList<AlertDto>> ExecuteByFieldAsync(
        string fieldId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Alert> alerts = await _alertStore.GetActiveByFieldAsync(fieldId, cancellationToken);
        return alerts.Select(AlertDto.FromEntity).ToList();
    }
}
