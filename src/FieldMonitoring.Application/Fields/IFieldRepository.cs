using FieldMonitoring.Domain.Fields;

namespace FieldMonitoring.Application.Fields;

/// <summary>
/// Repository para gerenciar o aggregate Field.
/// Abstração orientada ao domínio que trabalha com agregados completos.
/// </summary>
public interface IFieldRepository
{
    /// <summary>
    /// Obtém um talhão completo (com status, alertas e estado de regras).
    /// </summary>
    Task<Field?> GetByIdAsync(string fieldId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os talhões de uma fazenda.
    /// </summary>
    Task<IReadOnlyList<Field>> GetByFarmAsync(string farmId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste o aggregate Field completo (status, alertas, estado de regras).
    /// Garante consistência transacional.
    /// </summary>
    Task SaveAsync(Field field, CancellationToken cancellationToken = default);
}
