namespace FieldMonitoring.Domain.Rules;

/// <summary>
/// Representa uma regra de negócio configurável para avaliação de alertas.
/// Permite customizar limiares e janelas de tempo sem alterar código.
/// </summary>
public class Rule
{
    /// <summary>
    /// Identificador único da regra (Chave Primária).
    /// </summary>
    public Guid RuleId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tipo da regra (Dryness, PestRisk, ExtremeHeat, Frost, DryAir, HumidAir).
    /// Determina qual avaliador usará esta regra.
    /// </summary>
    public RuleType RuleType { get; set; }

    /// <summary>
    /// Indica se a regra está atualmente habilitada.
    /// Regras desabilitadas não geram alertas.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Valor limite para a regra (ex: 30.0 para 30% de umidade, 40.0 para 40°C).
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Janela de tempo em horas para avaliação da regra (ex: 24 horas).
    /// A condição deve persistir por este período para gerar alerta.
    /// </summary>
    public int WindowHours { get; set; }

    /// <summary>
    /// Timestamp da última atualização da regra.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Cria uma regra de seca padrão (umidade do solo < 30% por 24 horas).
    /// </summary>
    public static Rule CreateDefaultDrynessRule()
    {
        return new Rule
        {
            RuleType = RuleType.Dryness,
            IsEnabled = true,
            Threshold = 30.0,
            WindowHours = 24,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Cria uma regra de calor extremo padrão (temperatura do ar > 40°C por 4 horas).
    /// </summary>
    public static Rule CreateDefaultExtremeHeatRule()
    {
        return new Rule
        {
            RuleType = RuleType.ExtremeHeat,
            IsEnabled = true,
            Threshold = 40.0,
            WindowHours = 4,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Cria uma regra de geada padrão (temperatura do ar < 2°C por 2 horas).
    /// </summary>
    public static Rule CreateDefaultFrostRule()
    {
        return new Rule
        {
            RuleType = RuleType.Frost,
            IsEnabled = true,
            Threshold = 2.0,
            WindowHours = 2,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Cria uma regra de ar seco padrão (umidade do ar < 20% por 6 horas).
    /// </summary>
    public static Rule CreateDefaultDryAirRule()
    {
        return new Rule
        {
            RuleType = RuleType.DryAir,
            IsEnabled = true,
            Threshold = 20.0,
            WindowHours = 6,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Cria uma regra de ar úmido padrão (umidade do ar > 90% por 12 horas).
    /// </summary>
    public static Rule CreateDefaultHumidAirRule()
    {
        return new Rule
        {
            RuleType = RuleType.HumidAir,
            IsEnabled = true,
            Threshold = 90.0,
            WindowHours = 12,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Retorna uma descrição legível da regra.
    /// </summary>
    public string GetDescription()
    {
        return RuleType switch
        {
            RuleType.Dryness => $"Umidade do solo < {Threshold}% por {WindowHours} horas",
            RuleType.PestRisk => $"Condições de risco de praga por {WindowHours} horas",
            RuleType.ExtremeHeat => $"Temperatura do ar > {Threshold}°C por {WindowHours} horas",
            RuleType.Frost => $"Temperatura do ar < {Threshold}°C por {WindowHours} horas",
            RuleType.DryAir => $"Umidade do ar < {Threshold}% por {WindowHours} horas",
            RuleType.HumidAir => $"Umidade do ar > {Threshold}% por {WindowHours} horas",
            _ => $"Regra: limite {Threshold}, janela {WindowHours}h"
        };
    }
}
