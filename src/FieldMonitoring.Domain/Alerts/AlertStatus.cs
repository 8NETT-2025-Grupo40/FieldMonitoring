namespace FieldMonitoring.Domain.Alerts;

/// <summary>
/// Representa o status do ciclo de vida de um alerta.
/// </summary>
public enum AlertStatus
{
    /// <summary>
    /// A condição de alerta está atualmente ativa.
    /// </summary>
    Active = 1,

    /// <summary>
    /// A condição de alerta cessou e o alerta foi resolvido.
    /// </summary>
    Resolved = 2
}
