namespace FieldMonitoring.Application.Fields;

/// <summary>
/// DTO para resposta de visão geral da fazenda.
/// </summary>
public sealed record FarmOverviewDto
{
    /// <summary>
    /// Identificador da fazenda.
    /// </summary>
    public required string FarmId { get; init; }

    /// <summary>
    /// Número total de talhões na fazenda.
    /// </summary>
    public required int TotalFields { get; init; }

    /// <summary>
    /// Número total de alertas ativos em todos os talhões.
    /// </summary>
    public required int TotalActiveAlerts { get; init; }

    /// <summary>
    /// Visão geral de cada talhão na fazenda.
    /// </summary>
    public required IReadOnlyList<FieldOverviewDto> Fields { get; init; }
}
