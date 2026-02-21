namespace FieldMonitoring.Domain.Rules;

/// <summary>
/// Fornece o conjunto padrão de regras de alerta do domínio.
/// </summary>
public sealed class DefaultRuleSetProvider : IRuleSetProvider
{
    /// <summary>
    /// Retorna as regras padrão de processamento.
    /// </summary>
    public IReadOnlyList<Rule> GetRules()
    {
        return
        [
            Rule.CreateDefaultDrynessRule(),
            Rule.CreateDefaultExtremeHeatRule(),
            Rule.CreateDefaultFrostRule(),
            Rule.CreateDefaultDryAirRule(),
            Rule.CreateDefaultHumidAirRule()
        ];
    }
}
