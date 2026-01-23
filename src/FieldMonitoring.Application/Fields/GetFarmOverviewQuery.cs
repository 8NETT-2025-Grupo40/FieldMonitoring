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
        IReadOnlyList<Field> fields = await _fieldRepository.GetByFarmAsync(farmId, cancellationToken);

        List<FieldOverviewDto> fieldOverviews = new List<FieldOverviewDto>();
        var totalActiveAlerts = 0;

        foreach (Field field in fields)
        {
            var activeAlertCount = await _alertStore.CountActiveByFieldAsync(field.FieldId, cancellationToken);
            totalActiveAlerts += activeAlertCount;

            fieldOverviews.Add(new FieldOverviewDto
            {
                FieldId = field.FieldId,
                FarmId = field.FarmId,
                SensorId = field.SensorId,
                Status = field.Status,
                StatusReason = field.StatusReason,
                LastReadingAt = field.LastReadingAt,
                LastSoilHumidity = field.LastSoilMoisture?.Percent,
                LastSoilTemperature = field.LastSoilTemperature?.Celsius,
                LastAirTemperature = field.LastAirTemperature?.Celsius,
                LastAirHumidity = field.LastAirHumidity?.Percent,
                LastRainMm = field.LastRain?.Millimeters,
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
