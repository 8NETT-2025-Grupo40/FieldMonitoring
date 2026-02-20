namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Value Object representando temperatura em graus Celsius.
/// Encapsula validação de range realista para agricultura.
/// </summary>
public record Temperature
{
    /// <summary>
    /// Temperatura em graus Celsius.
    /// </summary>
    public double Celsius { get; }

    private Temperature(double celsius)
    {
        Celsius = celsius;
    }

    /// <summary>
    /// Cria uma instância de Temperature a partir de graus Celsius.
    /// </summary>
    /// <param name="celsius">Temperatura em °C (-50 a 60).</param>
    /// <returns>Result contendo Temperature se válido, ou erro.</returns>
    public static Result<Temperature> FromCelsius(double celsius)
    {
        if (celsius < -50 || celsius > 60)
        {
            return Result<Temperature>.Failure(
                $"Temperatura deve estar entre -50°C e 60°C, recebido: {celsius}°C");
        }

        return Result<Temperature>.Success(new Temperature(celsius));
    }

    public override string ToString() => $"{Celsius:F1}°C";
}
