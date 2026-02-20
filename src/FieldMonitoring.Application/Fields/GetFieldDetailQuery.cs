using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Domain.Fields;

namespace FieldMonitoring.Application.Fields;

/// <summary>
/// Query para obter informações detalhadas sobre um talhão.
/// </summary>
public class GetFieldDetailQuery
{
    private readonly IFieldRepository _fieldRepository;

    public GetFieldDetailQuery(IFieldRepository fieldRepository)
    {
        _fieldRepository = fieldRepository;
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

        return FieldSummaryDto.FromField<FieldDetailDto>(field) with
        {
            ActiveAlerts = field.Alerts.Select(AlertDto.FromEntity).ToList(),
            UpdatedAt = field.UpdatedAt
        };
    }
}
