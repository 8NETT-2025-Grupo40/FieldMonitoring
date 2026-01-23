using FieldMonitoring.Domain.Alerts;

namespace FieldMonitoring.Application.Alerts;

/// <summary>
/// Query para obter histórico de alertas.
/// </summary>
public class GetAlertHistoryQuery
{
    private readonly IAlertStore _alertStore;

    public GetAlertHistoryQuery(IAlertStore alertStore)
    {
        _alertStore = alertStore;
    }

    /// <summary>
    /// Obtém histórico de alertas de uma fazenda.
    /// </summary>
    public async Task<IReadOnlyList<AlertDto>> ExecuteByFarmAsync(
        string farmId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Alert> alerts = await _alertStore.GetByFarmAsync(farmId, from, to, cancellationToken);
        return alerts.Select(AlertDto.FromEntity).ToList();
    }

    /// <summary>
    /// Obtém histórico de alertas de um talhão.
    /// </summary>
    public async Task<IReadOnlyList<AlertDto>> ExecuteByFieldAsync(
        string fieldId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Alert> alerts = await _alertStore.GetByFieldAsync(fieldId, from, to, cancellationToken);
        return alerts.Select(AlertDto.FromEntity).ToList();
    }
}
