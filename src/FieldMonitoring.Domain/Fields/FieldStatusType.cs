namespace FieldMonitoring.Domain.Fields;

/// <summary>
/// Representa o status atual de um talhão com base nas leituras de sensor e regras de negócio.
/// </summary>
public enum FieldStatusType
{
    /// <summary>
    /// Todas as condições estão dentro dos parâmetros esperados.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Umidade do solo abaixo do limite pela janela de tempo configurada.
    /// </summary>
    DryAlert = 1,

    /// <summary>
    /// Nenhum dado de telemetria recebido dentro da janela de tempo esperada.
    /// </summary>
    NoRecentData = 3,

    /// <summary>
    /// Temperatura do ar acima do limite pela janela de tempo configurada.
    /// </summary>
    HeatAlert = 4,

    /// <summary>
    /// Temperatura do ar abaixo do limite (risco de geada).
    /// </summary>
    FrostAlert = 5,

    /// <summary>
    /// Umidade do ar abaixo do limite pela janela de tempo configurada.
    /// </summary>
    DryAirAlert = 6,

    /// <summary>
    /// Umidade do ar acima do limite pela janela de tempo configurada.
    /// </summary>
    HumidAirAlert = 7
}
