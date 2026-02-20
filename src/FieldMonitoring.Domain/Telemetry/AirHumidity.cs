namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Value Object representando umidade do ar em porcentagem (0-100).
/// </summary>
public record AirHumidity
{
    private const double MinPercent = 0;
    private const double MaxPercent = 100;

    /// <summary>
    /// Percentual de umidade (0-100).
    /// </summary>
    public double Percent { get; }

    private AirHumidity(double percent)
    {
        Percent = percent;
    }

    /// <summary>
    /// Cria uma instancia validada a partir de um percentual.
    /// </summary>
    public static Result<AirHumidity> FromPercent(double percent)
    {
        if (percent < MinPercent || percent > MaxPercent)
        {
            return Result<AirHumidity>.Failure(
                $"Umidade do ar deve estar entre 0 e 100%, recebido: {percent}%");
        }

        return Result<AirHumidity>.Success(new AirHumidity(percent));
    }

    public bool IsBelow(AirHumidity threshold) => Percent < threshold.Percent;
    public bool IsAbove(AirHumidity threshold) => Percent > threshold.Percent;
    public bool IsAtOrBelow(AirHumidity threshold) => Percent <= threshold.Percent;
    public bool IsAtOrAbove(AirHumidity threshold) => Percent >= threshold.Percent;

    public override string ToString() => $"{Percent:F1}%";
}
