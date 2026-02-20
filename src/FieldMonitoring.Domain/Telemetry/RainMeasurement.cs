namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Value Object representando medicao de chuva em milimetros (>= 0).
/// </summary>
public record RainMeasurement
{
    /// <summary>
    /// Volume em milímetros.
    /// </summary>
    public double Millimeters { get; }

    private RainMeasurement(double millimeters)
    {
        Millimeters = millimeters;
    }

    /// <summary>
    /// Cria uma instancia validada a partir de milimetros.
    /// </summary>
    public static Result<RainMeasurement> FromMillimeters(double millimeters)
    {
        if (millimeters < 0)
        {
            return Result<RainMeasurement>.Failure(
                $"Quantidade de chuva não pode ser negativa, recebido: {millimeters}mm");
        }

        return Result<RainMeasurement>.Success(new RainMeasurement(millimeters));
    }

    public override string ToString() => $"{Millimeters:F1}mm";
}
