namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Utilitário compartilhado para normalização de timestamps do InfluxDB.
/// O InfluxDB armazena timestamps em UTC; este helper garante a conversão correta.
/// </summary>
internal static class InfluxTimestampHelper
{
    /// <summary>
    /// Converte <see cref="DateTimeOffset"/> para <see cref="DateTime"/> UTC
    /// para gravação no InfluxDB.
    /// </summary>
    internal static DateTime NormalizeToUtc(DateTimeOffset timestamp)
    {
        return timestamp.UtcDateTime;
    }

    /// <summary>
    /// Converte <see cref="DateTime"/> para <see cref="DateTimeOffset"/> UTC,
    /// tratando os diferentes <see cref="DateTimeKind"/>.
    /// </summary>
    internal static DateTimeOffset NormalizeToUtc(DateTime timestamp)
    {
        if (timestamp.Kind == DateTimeKind.Utc)
        {
            return new DateTimeOffset(timestamp, TimeSpan.Zero);
        }

        if (timestamp.Kind == DateTimeKind.Local)
        {
            return new DateTimeOffset(timestamp.ToUniversalTime(), TimeSpan.Zero);
        }

        return new DateTimeOffset(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc));
    }
}
