namespace FieldMonitoring.Application.Fields;

/// <summary>
/// DTO para resposta de visão geral da fazenda.
/// </summary>
public sealed record FarmOverviewDto
{
    /// <summary>
    /// Identificador da fazenda.
    /// </summary>
    public string FarmId { get; set; } = string.Empty;

    /// <summary>
    /// Número total de talhões na fazenda.
    /// </summary>
    public int TotalFields { get; set; }

    /// <summary>
    /// Número total de alertas ativos em todos os talhões.
    /// </summary>
    public int TotalActiveAlerts { get; set; }

    /// <summary>
    /// Visão geral de cada talhão na fazenda.
    /// </summary>
    public required IReadOnlyList<FieldOverviewDto> Fields { get; init; }
}
