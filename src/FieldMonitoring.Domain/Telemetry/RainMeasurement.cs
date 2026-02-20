namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Value Object representando medição de chuva em milímetros.
/// Encapsula validação de valores não-negativos.
/// </summary>
public record RainMeasurement
{
    /// <summary>
    /// Quantidade de chuva em milímetros.
    /// </summary>
    public double Millimeters { get; }

    private RainMeasurement(double millimeters)
    {
        Millimeters = millimeters;
    }

    /// <summary>
    /// Cria uma instância de RainMeasurement a partir de milímetros.
    /// </summary>
    /// <param name="millimeters">Quantidade de chuva em mm (>= 0).</param>
    /// <returns>Result contendo RainMeasurement se válido, ou erro.</returns>
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
