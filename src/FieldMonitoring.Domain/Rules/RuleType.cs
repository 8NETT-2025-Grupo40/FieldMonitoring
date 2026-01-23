namespace FieldMonitoring.Domain.Rules;

/// <summary>
/// Represents the type of business rule for alert evaluation.
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Rule for detecting dry conditions based on soil moisture.
    /// </summary>
    Dryness = 1,

    /// <summary>
    /// Rule for detecting pest risk based on environmental conditions.
    /// </summary>
    PestRisk = 2,

    /// <summary>
    /// Rule for detecting extreme heat based on air temperature.
    /// </summary>
    ExtremeHeat = 3,

    /// <summary>
    /// Rule for detecting frost/freezing conditions based on air temperature.
    /// </summary>
    Frost = 4,

    /// <summary>
    /// Rule for detecting dry air conditions based on air humidity.
    /// </summary>
    DryAir = 5,

    /// <summary>
    /// Rule for detecting humid air conditions based on air humidity.
    /// </summary>
    HumidAir = 6
}
