namespace FieldMonitoring.Domain.Alerts;

/// <summary>
/// Representa o tipo de alerta que pode ser gerado para um talhão.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Alerta gerado quando a umidade do solo está abaixo do limite por período prolongado.
    /// </summary>
    Dryness = 1,

    /// <summary>
    /// Alerta gerado quando a temperatura do ar está acima do limite por período prolongado.
    /// </summary>
    ExtremeHeat = 3,

    /// <summary>
    /// Alerta gerado quando a temperatura do ar está abaixo do limite (risco de geada).
    /// </summary>
    Frost = 4,

    /// <summary>
    /// Alerta gerado quando a umidade do ar está abaixo do limite por período prolongado.
    /// </summary>
    DryAir = 5,

    /// <summary>
    /// Alerta gerado quando a umidade do ar está acima do limite por período prolongado.
    /// </summary>
    HumidAir = 6
}
