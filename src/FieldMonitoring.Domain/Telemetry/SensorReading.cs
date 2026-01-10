namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Representa uma leitura de sensor recebida via telemetria.
/// Value object imutável contendo todos os dados da leitura.
/// </summary>
public sealed record SensorReading
{
    /// <summary>
    /// Identificador único para controle de idempotência.
    /// Garante que a mesma leitura não seja processada duas vezes.
    /// </summary>
    public required string ReadingId { get; init; }

    /// <summary>
    /// Identificador do sensor que gerou a leitura.
    /// </summary>
    public required string SensorId { get; init; }

    /// <summary>
    /// Identificador do talhão onde a leitura foi coletada.
    /// </summary>
    public required string FieldId { get; init; }

    /// <summary>
    /// Identificador da fazenda à qual o talhão pertence.
    /// </summary>
    public required string FarmId { get; init; }

    /// <summary>
    /// Timestamp de quando a medição foi feita (hora do sensor, não de chegada).
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Umidade do solo (Value Object com validação 0-100%).
    /// Valor principal para avaliação da regra de seca.
    /// </summary>
    public required SoilMoisture SoilMoisture { get; init; }

    /// <summary>
    /// Temperatura do solo em graus Celsius (Value Object com validação -50 a 60°C).
    /// </summary>
    public required Temperature SoilTemperature { get; init; }

    /// <summary>
    /// Temperatura do ar em graus Celsius (Value Object com validação -50 a 60°C).
    /// Opcional - nem todos os sensores possuem este dado.
    /// </summary>
    public Temperature? AirTemperature { get; init; }

    /// <summary>
    /// Umidade do ar em porcentagem (Value Object com validação 0-100%).
    /// Opcional - nem todos os sensores possuem este dado.
    /// </summary>
    public AirHumidity? AirHumidity { get; init; }

    /// <summary>
    /// Precipitação em milímetros (Value Object com validação >= 0).
    /// </summary>
    public required RainMeasurement Rain { get; init; }

    /// <summary>
    /// Origem da leitura (HTTP ou MQTT).
    /// </summary>
    public ReadingSource Source { get; init; } = ReadingSource.Http;

    /// <summary>
    /// Cria uma SensorReading a partir de valores primitivos.
    /// Valida cada métrica e retorna Result com sucesso ou erro.
    /// </summary>
    public static Result<SensorReading> Create(
        string readingId,
        string sensorId,
        string fieldId,
        string farmId,
        DateTime timestamp,
        double soilMoisturePercent,
        double soilTemperatureC,
        double rainMm,
        double? airTemperatureC = null,
        double? airHumidityPercent = null,
        ReadingSource source = ReadingSource.Http)
    {
        // Validações de campos obrigatórios
        if (string.IsNullOrWhiteSpace(readingId))
            return Result<SensorReading>.Failure("ReadingId é obrigatório");

        if (string.IsNullOrWhiteSpace(sensorId))
            return Result<SensorReading>.Failure("SensorId é obrigatório");

        if (string.IsNullOrWhiteSpace(fieldId))
            return Result<SensorReading>.Failure("FieldId é obrigatório");

        if (string.IsNullOrWhiteSpace(farmId))
            return Result<SensorReading>.Failure("FarmId é obrigatório");

        // Validações de métricas usando Value Objects
        Result<SoilMoisture> soilMoistureResult = SoilMoisture.FromPercent(soilMoisturePercent);
        if (!soilMoistureResult.IsSuccess)
            return Result<SensorReading>.Failure(soilMoistureResult.Error!);

        Result<Temperature> soilTemperatureResult = Temperature.FromCelsius(soilTemperatureC);
        if (!soilTemperatureResult.IsSuccess)
            return Result<SensorReading>.Failure(soilTemperatureResult.Error!);

        Result<RainMeasurement> rainResult = RainMeasurement.FromMillimeters(rainMm);
        if (!rainResult.IsSuccess)
            return Result<SensorReading>.Failure(rainResult.Error!);

        // Validações de métricas opcionais
        Temperature? airTemperature = null;
        if (airTemperatureC.HasValue)
        {
            Result<Temperature> airTempResult = Temperature.FromCelsius(airTemperatureC.Value);
            if (!airTempResult.IsSuccess)
                return Result<SensorReading>.Failure(airTempResult.Error!);
            airTemperature = airTempResult.Value;
        }

        AirHumidity? airHumidity = null;
        if (airHumidityPercent.HasValue)
        {
            Result<AirHumidity> airHumidityResult = AirHumidity.FromPercent(airHumidityPercent.Value);
            if (!airHumidityResult.IsSuccess)
                return Result<SensorReading>.Failure(airHumidityResult.Error!);
            airHumidity = airHumidityResult.Value;
        }

        // Cria a leitura com Value Objects validados
        SensorReading reading = new SensorReading
        {
            ReadingId = readingId,
            SensorId = sensorId,
            FieldId = fieldId,
            FarmId = farmId,
            Timestamp = timestamp,
            SoilMoisture = soilMoistureResult.Value!,
            SoilTemperature = soilTemperatureResult.Value!,
            AirTemperature = airTemperature,
            AirHumidity = airHumidity,
            Rain = rainResult.Value!,
            Source = source
        };

        return Result<SensorReading>.Success(reading);
    }

    /// <summary>
    /// Valida os campos obrigatórios da leitura.
    /// Validações de métricas já são feitas na criação via Value Objects.
    /// </summary>
    public bool IsValid(out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(ReadingId))
        {
            errorMessage = "ReadingId é obrigatório";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SensorId))
        {
            errorMessage = "SensorId é obrigatório";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FieldId))
        {
            errorMessage = "FieldId é obrigatório";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FarmId))
        {
            errorMessage = "FarmId é obrigatório";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
