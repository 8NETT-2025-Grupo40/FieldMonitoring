using FieldMonitoring.Domain.Alerts;

namespace FieldMonitoring.Application.Alerts;

/// <summary>
/// Port para persistência de alertas (ativos e histórico).
/// </summary>
public interface IAlertStore
{
    /// <summary>
    /// Obtém todos os alertas ativos de uma fazenda.
    /// </summary>
    Task<IReadOnlyList<Alert>> GetActiveByFarmAsync(string farmId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os alertas ativos de um talhão.
    /// </summary>
    Task<IReadOnlyList<Alert>> GetActiveByFieldAsync(string fieldId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém alertas de um talhão dentro de um período (ativos e resolvidos).
    /// </summary>
    Task<IReadOnlyList<Alert>> GetByFieldAsync(
        string fieldId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém alertas de uma fazenda dentro de um período (ativos e resolvidos).
    /// </summary>
    Task<IReadOnlyList<Alert>> GetByFarmAsync(
        string farmId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um alerta pelo seu identificador.
    /// </summary>
    Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta alertas ativos de um talhão.
    /// </summary>
    Task<int> CountActiveByFieldAsync(string fieldId, CancellationToken cancellationToken = default);
}
