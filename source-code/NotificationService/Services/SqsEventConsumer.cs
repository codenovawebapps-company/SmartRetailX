using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using NotificationService.Models.Events;
using System.Text.Json;

namespace NotificationService.Services;

public class SqsEventConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IConfiguration _configuration;
    private readonly EventProcessor _eventProcessor;
    private readonly ILogger<SqsEventConsumer> _logger;
    private readonly string? _queueUrl;

    public SqsEventConsumer(
        IAmazonSQS sqsClient,
        IConfiguration configuration,
        EventProcessor eventProcessor,
        ILogger<SqsEventConsumer> logger)
    {
        _sqsClient = sqsClient;
        _configuration = configuration;
        _eventProcessor = eventProcessor;
        _logger = logger;
        _queueUrl = _configuration["AWS:QueueUrl"] ?? _configuration["SQS_QUEUE_URL"];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_queueUrl))
        {
            _logger.LogWarning("SQS Queue URL not configured (AWS:QueueUrl or SQS_QUEUE_URL). SQS event consumption background task is suspended.");
            return;
        }

        _logger.LogInformation("Starting SQS background consumer polling queue: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 10 // long polling
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                foreach (var message in response.Messages)
                {
                    bool processSuccess = await ProcessMessageAsync(message);

                    if (processSuccess)
                    {
                        var deleteRequest = new DeleteMessageRequest
                        {
                            QueueUrl = _queueUrl,
                            ReceiptHandle = message.ReceiptHandle
                        };
                        await _sqsClient.DeleteMessageAsync(deleteRequest, stoppingToken);
                        _logger.LogDebug("Deleted message {MessageId} from SQS queue", message.MessageId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // normal cancellation on shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SQS consumer loop. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessMessageAsync(Message message)
    {
        try
        {
            _logger.LogDebug("Received SQS Message ID: {MessageId}", message.MessageId);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var envelope = JsonSerializer.Deserialize<SqsEnvelope>(message.Body, options);

            if (envelope == null)
            {
                _logger.LogError("Invalid SQS message body. Could not deserialize into EventBridge envelope. Body: {Body}", message.Body);
                return true; // delete malformed message
            }

            return await _eventProcessor.ProcessEnvelopeAsync(envelope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process SQS message: {MessageId}", message.MessageId);
            return false; // Leave on queue to retry
        }
    }
}
