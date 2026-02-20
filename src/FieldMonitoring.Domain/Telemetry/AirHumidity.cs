namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Value Object representando umidade do ar em porcentagem.
/// Encapsula validação e semântica de comparação.
/// </summary>
public record AirHumidity
{
    /// <summary>
    /// Percentual de umidade do ar (0-100%).
    /// </summary>
    public double Percent { get; }

    private AirHumidity(double percent)
    {
        Percent = percent;
    }

    /// <summary>
    /// Cria uma instância de AirHumidity a partir de um percentual.
    /// </summary>
    /// <param name="percent">Percentual de umidade (0-100).</param>
    /// <returns>Result contendo AirHumidity se válido, ou erro.</returns>
    public static Result<AirHumidity> FromPercent(double percent)
    {
        if (percent < 0 || percent > 100)
        {
            return Result<AirHumidity>.Failure(
                $"Umidade do ar deve estar entre 0 e 100%, recebido: {percent}%");
        }

        return Result<AirHumidity>.Success(new AirHumidity(percent));
    }

    /// <summary>
    /// Verifica se a umidade está abaixo de um threshold.
    /// </summary>
    /// <param name="threshold">Threshold para comparação.</param>
    /// <returns>True se está abaixo do threshold.</returns>
    public bool IsBelow(AirHumidity threshold) => Percent < threshold.Percent;

    /// <summary>
    /// Verifica se a umidade está acima de um threshold.
    /// </summary>
    /// <param name="threshold">Threshold para comparação.</param>
    /// <returns>True se está acima do threshold.</returns>
    public bool IsAbove(AirHumidity threshold) => Percent > threshold.Percent;

    public override string ToString() => $"{Percent:F1}%";
}
