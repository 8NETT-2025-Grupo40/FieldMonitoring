namespace FieldMonitoring.Domain.Rules;

/// <summary>
/// Representa uma regra de negócio para avaliação de alertas.
/// Permite customizar limiares e janelas de tempo sem alterar código.
/// </summary>
public class Rule
{
    /// <summary>
    /// Construtor privado - força uso de factory methods.
    /// </summary>
    private Rule() { }

    /// <summary>
    /// Identificador único da regra (Chave Primária).
    /// </summary>
    public Guid RuleId { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Tipo da regra (Dryness, ExtremeHeat, Frost, DryAir, HumidAir).
    /// Determina qual avaliador usará esta regra.
    /// </summary>
    public RuleType RuleType { get; private set; }

    /// <summary>
    /// Indica se a regra está atualmente habilitada.
    /// Regras desabilitadas não geram alertas.
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Valor limite para a regra (ex: 30.0 para 30% de umidade, 40.0 para 40°C).
    /// </summary>
    public double Threshold { get; private set; }

    /// <summary>
    /// Janela de tempo em horas para avaliação da regra (ex: 24 horas).
    /// A condição deve persistir por este período para gerar alerta.
    /// </summary>
    public int WindowHours { get; private set; }

    /// <summary>
    /// Timestamp da última atualização da regra.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Cria uma nova regra de negócio com os parâmetros especificados.
    /// </summary>
    /// <param name="ruleType">Tipo da regra.</param>
    /// <param name="threshold">Valor limite para a regra.</param>
    /// <param name="windowHours">Janela de tempo em horas.</param>
    /// <param name="isEnabled">Se a regra está habilitada (padrão: true).</param>
    public static Rule Create(RuleType ruleType, double threshold, int windowHours, bool isEnabled = true)
    {
        return new Rule
        {
            RuleType = ruleType,
            Threshold = threshold,
            WindowHours = windowHours,
            IsEnabled = isEnabled,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Cria uma regra de seca padrão (umidade do solo &lt; 30% por 24 horas).
    /// </summary>
    public static Rule CreateDefaultDrynessRule()
        => Create(RuleType.Dryness, threshold: 30.0, windowHours: 24);

    /// <summary>
    /// Cria uma regra de calor extremo padrão (temperatura do ar &gt; 40°C por 4 horas).
    /// </summary>
    public static Rule CreateDefaultExtremeHeatRule()
        => Create(RuleType.ExtremeHeat, threshold: 40.0, windowHours: 4);

    /// <summary>
    /// Cria uma regra de geada padrão (temperatura do ar &lt; 2°C por 2 horas).
    /// </summary>
    public static Rule CreateDefaultFrostRule()
        => Create(RuleType.Frost, threshold: 2.0, windowHours: 2);

    /// <summary>
    /// Cria uma regra de ar seco padrão (umidade do ar &lt; 20% por 6 horas).
    /// </summary>
    public static Rule CreateDefaultDryAirRule()
        => Create(RuleType.DryAir, threshold: 20.0, windowHours: 6);

    /// <summary>
    /// Cria uma regra de ar úmido padrão (umidade do ar &gt; 90% por 12 horas).
    /// </summary>
    public static Rule CreateDefaultHumidAirRule()
        => Create(RuleType.HumidAir, threshold: 90.0, windowHours: 12);

    /// <summary>
    /// Retorna uma descrição legível da regra.
    /// </summary>
    public string GetDescription()
    {
        return RuleType switch
        {
            RuleType.Dryness => $"Umidade do solo < {Threshold}% por {WindowHours} horas",
            RuleType.ExtremeHeat => $"Temperatura do ar > {Threshold}°C por {WindowHours} horas",
            RuleType.Frost => $"Temperatura do ar < {Threshold}°C por {WindowHours} horas",
            RuleType.DryAir => $"Umidade do ar < {Threshold}% por {WindowHours} horas",
            RuleType.HumidAir => $"Umidade do ar > {Threshold}% por {WindowHours} horas",
            _ => $"Regra: limite {Threshold}, janela {WindowHours}h"
        };
    }
}
