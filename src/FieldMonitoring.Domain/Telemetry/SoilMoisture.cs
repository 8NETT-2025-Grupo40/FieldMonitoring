namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Value Object representando umidade do solo em porcentagem (0-100).
/// </summary>
public record SoilMoisture
{
    private const double MinPercent = 0;
    private const double MaxPercent = 100;

    /// <summary>
    /// Percentual de umidade (0-100).
    /// </summary>
    public double Percent { get; }

    private SoilMoisture(double percent)
    {
        Percent = percent;
    }

    /// <summary>
    /// Cria uma instancia validada a partir de um percentual.
    /// </summary>
    public static Result<SoilMoisture> FromPercent(double percent)
    {
        if (percent < MinPercent || percent > MaxPercent)
        {
            return Result<SoilMoisture>.Failure(
                $"Umidade do solo deve estar entre 0 e 100%, recebido: {percent}%");
        }

        return Result<SoilMoisture>.Success(new SoilMoisture(percent));
    }

    public bool IsBelow(SoilMoisture threshold) => Percent < threshold.Percent;
    public bool IsAbove(SoilMoisture threshold) => Percent > threshold.Percent;
    public bool IsAtOrBelow(SoilMoisture threshold) => Percent <= threshold.Percent;
    public bool IsAtOrAbove(SoilMoisture threshold) => Percent >= threshold.Percent;

    public override string ToString() => $"{Percent:F1}%";
}
