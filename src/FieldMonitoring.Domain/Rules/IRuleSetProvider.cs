namespace FieldMonitoring.Domain.Rules;

/// <summary>
/// Define como obter o conjunto de regras de negócio aplicável ao processamento.
/// </summary>
public interface IRuleSetProvider
{
    /// <summary>
    /// Retorna as regras aplicáveis ao processamento.
    /// </summary>
    IReadOnlyList<Rule> GetRules();
}
