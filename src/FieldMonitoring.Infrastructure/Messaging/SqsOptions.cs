namespace FieldMonitoring.Infrastructure.Messaging;

/// <summary>
/// Opções de configuração para mensageria SQS.
/// </summary>
public class SqsOptions
{
    public const string SectionName = "Sqs";

    /// <summary>
    /// Região AWS para SQS.
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// URL da fila SQS para mensagens de telemetria.
    /// </summary>
    public string QueueUrl { get; set; } = string.Empty;

    /// <summary>
    /// Número máximo de mensagens para receber por consulta.
    /// </summary>
    public int MaxNumberOfMessages { get; set; } = 10;

    /// <summary>
    /// Tempo de espera em segundos para long polling.
    /// </summary>
    public int WaitTimeSeconds { get; set; } = 20;

    /// <summary>
    /// Timeout de visibilidade em segundos.
    /// </summary>
    public int VisibilityTimeout { get; set; } = 30;

    /// <summary>
    /// Se o consumidor SQS está habilitado.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
