namespace FieldMonitoring.Domain.Alerts;

/// <summary>
/// Represents the lifecycle status of an alert.
/// </summary>
public enum AlertStatus
{
    /// <summary>
    /// The alert condition is currently active.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The alert condition has ceased and the alert was resolved.
    /// </summary>
    Resolved = 2
}
