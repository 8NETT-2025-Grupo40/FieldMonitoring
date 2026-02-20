using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Domain.Fields;

namespace FieldMonitoring.Application.Fields;

/// <summary>
/// Query para obter informações detalhadas sobre um talhão.
/// </summary>
public class GetFieldDetailQuery
{
    private readonly IFieldRepository _fieldRepository;
    private readonly IAlertStore _alertStore;

    public GetFieldDetailQuery(IFieldRepository fieldRepository, IAlertStore alertStore)
    {
        _fieldRepository = fieldRepository;
        _alertStore = alertStore;
    }

    /// <summary>
    /// Executa a query para obter detalhes de um talhão.
    /// </summary>
    public async Task<FieldDetailDto?> ExecuteAsync(
        string fieldId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);

        Field? field = await _fieldRepository.GetByIdAsync(fieldId, cancellationToken);
        if (field == null)
        {
            return null;
        }

        IReadOnlyList<Domain.Alerts.Alert> activeAlerts =
            await _alertStore.GetActiveByFieldAsync(fieldId, cancellationToken);

        return FieldSummaryDto.FromField<FieldDetailDto>(field) with
        {
            ActiveAlerts = activeAlerts.Select(AlertDto.FromEntity).ToList(),
            UpdatedAt = field.UpdatedAt
        };
    }
}
