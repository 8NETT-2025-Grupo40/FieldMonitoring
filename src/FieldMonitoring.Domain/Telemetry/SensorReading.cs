namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Leitura de sensor recebida via telemetria (imutavel).
/// </summary>
public sealed record SensorReading
{
    private SensorReading() { }

    public string ReadingId { get; init; } = null!;
    public string SensorId { get; init; } = null!;
    public string FieldId { get; init; } = null!;
    public string FarmId { get; init; } = null!;
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Umidade do solo (obrigatório).
    /// </summary>
    public SoilMoisture SoilMoisture { get; init; } = null!;

    /// <summary>
    /// Temperatura do solo (obrigatório).
    /// </summary>
    public Temperature SoilTemperature { get; init; } = null!;

    /// <summary>
    /// Temperatura do ar (opcional - nem todos os sensores possuem).
    /// </summary>
    public Temperature? AirTemperature { get; init; }

    /// <summary>
    /// Umidade do ar (opcional - nem todos os sensores possuem).
    /// </summary>
    public AirHumidity? AirHumidity { get; init; }

    /// <summary>
    /// Volume de chuva (obrigatório).
    /// </summary>
    public RainMeasurement Rain { get; init; } = null!;

    public ReadingSource Source { get; init; } = ReadingSource.Http;

    /// <summary>
    /// Cria uma SensorReading a partir de valores primitivos, validando cada metrica.
    /// </summary>
    public static Result<SensorReading> Create(
        string readingId,
        string sensorId,
        string fieldId,
        string farmId,
        DateTimeOffset timestamp,
        double soilMoisturePercent,
        double soilTemperatureC,
        double rainMm,
        double? airTemperatureC = null,
        double? airHumidityPercent = null,
        ReadingSource source = ReadingSource.Http)
    {
        if (string.IsNullOrWhiteSpace(readingId))
            return Result<SensorReading>.Failure("ReadingId é obrigatório");

        if (string.IsNullOrWhiteSpace(sensorId))
            return Result<SensorReading>.Failure("SensorId é obrigatório");

        if (string.IsNullOrWhiteSpace(fieldId))
            return Result<SensorReading>.Failure("FieldId é obrigatório");

        if (string.IsNullOrWhiteSpace(farmId))
            return Result<SensorReading>.Failure("FarmId é obrigatório");

        Result<SoilMoisture> soilMoistureResult = SoilMoisture.FromPercent(soilMoisturePercent);
        if (!soilMoistureResult.IsSuccess)
            return Result<SensorReading>.Failure(soilMoistureResult.Error!);

        Result<Temperature> soilTemperatureResult = Temperature.FromCelsius(soilTemperatureC);
        if (!soilTemperatureResult.IsSuccess)
            return Result<SensorReading>.Failure(soilTemperatureResult.Error!);

        Result<RainMeasurement> rainResult = RainMeasurement.FromMillimeters(rainMm);
        if (!rainResult.IsSuccess)
            return Result<SensorReading>.Failure(rainResult.Error!);

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
}
