namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// Status do processamento de uma leitura.
/// </summary>
public enum ProcessingStatus
{
    Success,
    Skipped,
    Failed
}

/// <summary>
/// Resultado do processamento de uma leitura de telemetria.
/// </summary>
public sealed record ProcessingResult(ProcessingStatus Status, string? Message = null, bool ShouldRetry = false)
{
    public bool IsSuccess => Status is ProcessingStatus.Success or ProcessingStatus.Skipped;

    public bool WasSkipped => Status is ProcessingStatus.Skipped;

    public static ProcessingResult Success(string? message = null)
        => new(ProcessingStatus.Success, message);

    public static ProcessingResult Skipped(string reason)
        => new(ProcessingStatus.Skipped, reason);

    public static ProcessingResult RetryableFailure(string reason)
        => new(ProcessingStatus.Failed, reason, ShouldRetry: true);

    public static ProcessingResult NonRetryableFailure(string reason)
        => new(ProcessingStatus.Failed, reason, ShouldRetry: false);
}
