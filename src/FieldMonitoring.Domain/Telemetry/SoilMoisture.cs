namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Value Object representando umidade do solo em porcentagem.
/// Encapsula validação e semântica de comparação.
/// </summary>
public record SoilMoisture
{
    /// <summary>
    /// Percentual de umidade do solo (0-100%).
    /// </summary>
    public double Percent { get; }

    private SoilMoisture(double percent)
    {
        Percent = percent;
    }

    /// <summary>
    /// Cria uma instância de SoilMoisture a partir de um percentual.
    /// </summary>
    /// <param name="percent">Percentual de umidade (0-100).</param>
    /// <returns>Result contendo SoilMoisture se válido, ou erro.</returns>
    public static Result<SoilMoisture> FromPercent(double percent)
    {
        if (percent < 0 || percent > 100)
        {
            return Result<SoilMoisture>.Failure(
                $"Umidade do solo deve estar entre 0 e 100%, recebido: {percent}%");
        }

        return Result<SoilMoisture>.Success(new SoilMoisture(percent));
    }

    /// <summary>
    /// Verifica se a umidade está abaixo de um threshold.
    /// </summary>
    /// <param name="threshold">Threshold para comparação.</param>
    /// <returns>True se está abaixo do threshold.</returns>
    public bool IsBelow(SoilMoisture threshold) => Percent < threshold.Percent;

    /// <summary>
    /// Verifica se a umidade está acima de um threshold.
    /// </summary>
    /// <param name="threshold">Threshold para comparação.</param>
    /// <returns>True se está acima do threshold.</returns>
    public bool IsAbove(SoilMoisture threshold) => Percent > threshold.Percent;

    public override string ToString() => $"{Percent:F1}%";
}
