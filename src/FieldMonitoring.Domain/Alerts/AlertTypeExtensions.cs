using FieldMonitoring.Domain.Fields;

namespace FieldMonitoring.Domain.Alerts;

/// <summary>
/// Extension methods para AlertType.
/// Centraliza severidade e mapeamentos para evitar switches espalhados pelo código.
/// </summary>
public static class AlertTypeExtensions
{
    /// <summary>
    /// Retorna a severidade do tipo de alerta (menor = mais crítico).
    /// Usado para priorizar qual alerta exibir no status do Field.
    /// </summary>
    public static int GetSeverity(this AlertType type) => type switch
    {
        AlertType.Frost => 1,        // Mais crítico - dano imediato à plantação
        AlertType.ExtremeHeat => 2,  // Muito crítico - estresse térmico severo
        AlertType.Dryness => 3,      // Crítico - falta de água
        AlertType.DryAir => 4,       // Moderado - estresse hídrico
        AlertType.HumidAir => 5,     // Moderado - risco de doenças fúngicas
        AlertType.PestRisk => 6,     // Monitoramento - risco de pragas
        _ => 99
    };

    /// <summary>
    /// Converte AlertType para o FieldStatusType correspondente.
    /// </summary>
    public static FieldStatusType ToFieldStatus(this AlertType type) => type switch
    {
        AlertType.Frost => FieldStatusType.FrostAlert,
        AlertType.ExtremeHeat => FieldStatusType.HeatAlert,
        AlertType.Dryness => FieldStatusType.DryAlert,
        AlertType.DryAir => FieldStatusType.DryAirAlert,
        AlertType.HumidAir => FieldStatusType.HumidAirAlert,
        AlertType.PestRisk => FieldStatusType.PestRisk,
        _ => FieldStatusType.Normal
    };

    /// <summary>
    /// Retorna uma descrição padrão para o alerta quando não há reason específica.
    /// </summary>
    public static string GetDefaultReason(this AlertType type) => type switch
    {
        AlertType.Frost => "Alerta de geada ativo",
        AlertType.ExtremeHeat => "Alerta de calor extremo ativo",
        AlertType.Dryness => "Alerta de seca ativo",
        AlertType.DryAir => "Alerta de ar seco ativo",
        AlertType.HumidAir => "Alerta de ar úmido ativo",
        AlertType.PestRisk => "Alerta de risco de pragas ativo",
        _ => "Alerta ativo"
    };
}
