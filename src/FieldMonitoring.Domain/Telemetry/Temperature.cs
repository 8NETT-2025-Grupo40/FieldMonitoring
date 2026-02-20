namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Value Object representando temperatura em graus Celsius.
/// Range valido para agricultura: -50 a 60.
/// </summary>
public record Temperature
{
    private const double MinCelsius = -50;
    private const double MaxCelsius = 60;

    /// <summary>
    /// Valor em graus Celsius.
    /// </summary>
    public double Celsius { get; }

    private Temperature(double celsius)
    {
        Celsius = celsius;
    }

    /// <summary>
    /// Cria uma instancia validada a partir de graus Celsius.
    /// </summary>
    public static Result<Temperature> FromCelsius(double celsius)
    {
        if (celsius < MinCelsius || celsius > MaxCelsius)
        {
            return Result<Temperature>.Failure(
                $"Temperatura deve estar entre -50°C e 60°C, recebido: {celsius}°C");
        }

        return Result<Temperature>.Success(new Temperature(celsius));
    }

    public override string ToString() => $"{Celsius:F1}°C";

    public bool IsBelow(Temperature threshold) => Celsius < threshold.Celsius;
    public bool IsAbove(Temperature threshold) => Celsius > threshold.Celsius;
    public bool IsAtOrBelow(Temperature threshold) => Celsius <= threshold.Celsius;
    public bool IsAtOrAbove(Temperature threshold) => Celsius >= threshold.Celsius;
}
