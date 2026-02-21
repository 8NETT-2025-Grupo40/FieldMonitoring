using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Domain.Fields;

namespace FieldMonitoring.Application.Fields;

/// <summary>
/// Query para obter a visão geral de todos os talhões de uma fazenda.
/// </summary>
public class GetFarmOverviewQuery
{
    private readonly IFieldRepository _fieldRepository;
    private readonly IAlertStore _alertStore;

    public GetFarmOverviewQuery(
        IFieldRepository fieldRepository,
        IAlertStore alertStore)
    {
        _fieldRepository = fieldRepository;
        _alertStore = alertStore;
    }

    /// <summary>
    /// Executa a query para obter visão geral da fazenda.
    /// </summary>
    public async Task<FarmOverviewDto> ExecuteAsync(
        string farmId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(farmId);

        IReadOnlyList<Field> fields = await _fieldRepository.GetByFarmAsync(farmId, cancellationToken);

        var alertCounts = await _alertStore.CountActiveByFieldsAsync(
            fields.Select(f => f.FieldId),
            cancellationToken);

        var totalActiveAlerts = 0;
        List<FieldOverviewDto> fieldOverviews = new List<FieldOverviewDto>();

        foreach (Field field in fields)
        {
            var activeAlertCount = alertCounts.GetValueOrDefault(field.FieldId, 0);
            totalActiveAlerts += activeAlertCount;

            fieldOverviews.Add(FieldSummaryDto.FromField<FieldOverviewDto>(field) with
            {
                ActiveAlertCount = activeAlertCount
            });
        }

        return new FarmOverviewDto
        {
            FarmId = farmId,
            TotalFields = fieldOverviews.Count,
            TotalActiveAlerts = totalActiveAlerts,
            Fields = fieldOverviews
        };
    }
}
