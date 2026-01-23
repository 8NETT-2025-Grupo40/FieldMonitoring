using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FieldMonitoring.Application.Serialization;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldMonitoring.Infrastructure.Messaging;

/// <summary>
/// Serviço em background que consome mensagens de telemetria da fila SQS.
/// Processa cada mensagem usando o ProcessTelemetryReadingUseCase.
/// </summary>
public class SqsConsumerService : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new StrictDateTimeOffsetJsonConverter()
        }
    };
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAmazonSQS _sqsClient;
    private readonly SqsOptions _options;
    private readonly ILogger<SqsConsumerService> _logger;

    public SqsConsumerService(
        IServiceScopeFactory scopeFactory,
        IAmazonSQS sqsClient,
        IOptions<SqsOptions> options,
        ILogger<SqsConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _sqsClient = sqsClient;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("SQS Consumer desabilitado");
            return;
        }

        if (string.IsNullOrEmpty(_options.QueueUrl))
        {
            _logger.LogWarning("URL da fila SQS não configurada. Consumer não será iniciado.");
            return;
        }

        _logger.LogInformation("SQS Consumer iniciando. Fila: {QueueUrl}", _options.QueueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro processando mensagens SQS. Tentando novamente em 5 segundos...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Busca e processa lote de mensagens da fila SQS.
    /// </summary>
    private async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        ReceiveMessageRequest request = new ReceiveMessageRequest
        {
            QueueUrl = _options.QueueUrl,
            MaxNumberOfMessages = _options.MaxNumberOfMessages,
            WaitTimeSeconds = _options.WaitTimeSeconds,
            VisibilityTimeout = _options.VisibilityTimeout
        };

        ReceiveMessageResponse response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

        if (response.Messages.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Recebidas {Count} mensagens do SQS", response.Messages.Count);

        foreach (Message? message in response.Messages)
        {
            await ProcessSingleMessageAsync(message, stoppingToken);
        }
    }

    /// <summary>
    /// Processa uma única mensagem de telemetria.
    /// Em caso de sucesso, deleta a mensagem da fila.
    /// Em caso de falha, deixa a mensagem para retry via visibility timeout.
    /// </summary>
    private async Task ProcessSingleMessageAsync(Message message, CancellationToken stoppingToken)
    {
        try
        {
            TelemetryReceivedMessage? telemetryMessage = DeserializeMessage(message.Body);
            if (telemetryMessage == null)
            {
                _logger.LogWarning("Falha ao deserializar mensagem: {MessageId}", message.MessageId);
                // Deleta mensagem malformada para evitar reprocessamento infinito
                await DeleteMessageAsync(message, stoppingToken);
                return;
            }

            using IServiceScope scope = _scopeFactory.CreateScope();
            ProcessTelemetryReadingUseCase useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();

            ProcessingResult result = await useCase.ExecuteAsync(telemetryMessage, stoppingToken);

            if (result.IsSuccess)
            {
                _logger.LogDebug(
                    "Leitura {ReadingId} processada: {Message}",
                    telemetryMessage.ReadingId,
                    result.WasSkipped ? "Pulada (duplicada)" : "Sucesso");

                await DeleteMessageAsync(message, stoppingToken);
            }
            else
            {
                _logger.LogWarning(
                    "Falha ao processar leitura {ReadingId}: {Reason}",
                    telemetryMessage.ReadingId,
                    result.Message);

                // Não deleta - deixa retry via visibility timeout
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro processando mensagem {MessageId}", message.MessageId);
            // Não deleta - deixa retry via visibility timeout
        }
    }

    /// <summary>
    /// Deserializa o corpo da mensagem SQS para TelemetryReceivedMessage.
    /// Trata caso especial onde a mensagem vem encapsulada por SNS.
    /// </summary>
    private TelemetryReceivedMessage? DeserializeMessage(string body)
    {
        try
        {
            // Trata wrapper SNS se a mensagem vier de um tópico SNS
            if (body.Contains("\"Message\"") && body.Contains("\"TopicArn\""))
            {
                SnsMessageWrapper? snsWrapper = JsonSerializer.Deserialize<SnsMessageWrapper>(body);
                if (snsWrapper?.Message != null)
                {
                    body = snsWrapper.Message;
                }
            }

            return JsonSerializer.Deserialize<TelemetryReceivedMessage>(body, SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Falha ao deserializar corpo da mensagem");
            return null;
        }
    }

    /// <summary>
    /// Remove a mensagem da fila SQS após processamento bem-sucedido.
    /// </summary>
    private async Task DeleteMessageAsync(Message message, CancellationToken stoppingToken)
    {
        try
        {
            await _sqsClient.DeleteMessageAsync(_options.QueueUrl, message.ReceiptHandle, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao deletar mensagem {MessageId}", message.MessageId);
        }
    }

    /// <summary>
    /// Wrapper para mensagens SNS quando a fila SQS está inscrita em um tópico SNS.
    /// </summary>
    private class SnsMessageWrapper
    {
        public string? Message { get; set; }
        public string? TopicArn { get; set; }
    }
}
