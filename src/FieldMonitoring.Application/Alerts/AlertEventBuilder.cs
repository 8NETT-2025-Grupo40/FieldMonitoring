using FieldMonitoring.Domain.Alerts;

namespace FieldMonitoring.Application.Alerts;

/// <summary>
/// Responsável por construir eventos de alerta a partir de mudanças de status.
/// </summary>
public static class AlertEventBuilder
{
    /// <summary>
    /// Captura o status atual de todos os alertas de um Field.
    /// </summary>
    public static Dictionary<Guid, AlertStatus> CaptureStatuses(IReadOnlyList<Alert> alerts)
    {
        return alerts.ToDictionary(a => a.AlertId, a => a.Status);
    }

    /// <summary>
    /// Compara os status anteriores com os atuais e gera eventos para alertas novos ou alterados.
    /// </summary>
    public static IReadOnlyList<AlertEvent> BuildEvents(
        IReadOnlyDictionary<Guid, AlertStatus> before,
        IReadOnlyList<Alert> after)
    {
        return after
            .Where(alert => !before.TryGetValue(alert.AlertId, out var prev) || prev != alert.Status)
            .Select(CreateEvent)
            .ToList();
    }

    /// <summary>
    /// Cria um AlertEvent a partir de um Alert.
    /// </summary>
    public static AlertEvent CreateEvent(Alert alert)
    {
        DateTimeOffset occurredAt = alert.Status == AlertStatus.Resolved
            ? alert.ResolvedAt ?? DateTimeOffset.UtcNow
            : alert.StartedAt;

        return new AlertEvent
        {
            AlertId = alert.AlertId,
            FarmId = alert.FarmId,
            FieldId = alert.FieldId,
            AlertType = alert.AlertType,
            Status = alert.Status,
            Reason = alert.Reason,
            Severity = alert.Severity,
            OccurredAt = occurredAt
        };
    }
}
