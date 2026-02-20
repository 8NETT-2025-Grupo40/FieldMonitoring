namespace FieldMonitoring.Domain.Rules;

/// <summary>
/// Representa o tipo de regra de negócio para avaliação de alertas.
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Regra para detecção de condições de seca baseada na umidade do solo.
    /// </summary>
    Dryness = 1,

    /// <summary>
    /// Regra para detecção de calor extremo baseada na temperatura do ar.
    /// </summary>
    ExtremeHeat = 3,

    /// <summary>
    /// Regra para detecção de geada/congelamento baseada na temperatura do ar.
    /// </summary>
    Frost = 4,

    /// <summary>
    /// Regra para detecção de ar seco baseada na umidade do ar.
    /// </summary>
    DryAir = 5,

    /// <summary>
    /// Regra para detecção de ar úmido baseada na umidade do ar.
    /// </summary>
    HumidAir = 6
}
