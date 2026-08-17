using Amazon.SQS;
using Amazon.SQS.Model;
using InventoryService.Models;
using System.Text.Json;

namespace InventoryService.Services;

public class SqsInventoryConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly InventoryService _inventoryService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqsInventoryConsumer> _logger;
    private readonly string? _queueUrl;

    public SqsInventoryConsumer(
        IAmazonSQS sqsClient,
        InventoryService inventoryService,
        IConfiguration configuration,
        ILogger<SqsInventoryConsumer> logger)
    {
        _sqsClient = sqsClient;
        _inventoryService = inventoryService;
        _configuration = configuration;
        _logger = logger;
        _queueUrl = _configuration["AWS:QueueUrl"] 
                    ?? _configuration["SQS_QUEUE_URL"]
                    ?? "https://sqs.ap-south-1.amazonaws.com/530751148786/SmartRetailX-InventoryQueue";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_queueUrl))
        {
            _logger.LogWarning("SQS Queue URL not configured. Inventory background consumer suspended.");
            return;
        }

        _logger.LogInformation("Starting Inventory SQS Background Consumer on {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20 // Long polling - more efficient and cost-effective
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (response.Messages != null && response.Messages.Count > 0)
                {
                    _logger.LogInformation("Received {Count} messages from SQS", response.Messages.Count);

                    foreach (var message in response.Messages)
                    {
                        bool success = ProcessMessage(message);

                        if (success)
                        {
                            await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                            {
                                QueueUrl = _queueUrl,
                                ReceiptHandle = message.ReceiptHandle
                            }, stoppingToken);

                            _logger.LogDebug("Deleted message {MessageId} from SQS queue", message.MessageId);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (AmazonSQSException ex) when (ex.ErrorCode == "AWS.SimpleQueueService.NonExistentQueue" || ex.ErrorCode == "AccessDeniedException")
            {
                _logger.LogWarning("SQS Access/Queue warning: {Message}. Retrying in 30 seconds...", ex.Message);
                await Task.Delay(30000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SQS inventory consumer loop. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private bool ProcessMessage(Message message)
    {
        try
        {
            _logger.LogInformation("Processing SQS Message ID: {MessageId}", message.MessageId);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            OrderCreatedEvent? orderEvent = null;

            // Attempt 1: Check if this is an EventBridge Envelope {"detail-type": "...", "detail": {...}}
            using var doc = JsonDocument.Parse(message.Body);
            if (doc.RootElement.TryGetProperty("detail", out var detailElement))
            {
                orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(detailElement.GetRawText(), options);
            }
            else
            {
                // Attempt 2: Direct OrderCreatedEvent payload
                orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Body, options);
            }

            if (orderEvent != null && !string.IsNullOrWhiteSpace(orderEvent.ProductId))
            {
                return _inventoryService.ProcessOrderCreatedEvent(orderEvent);
            }

            _logger.LogWarning("Message {MessageId} could not be parsed into OrderCreatedEvent. Body: {Body}", 
                message.MessageId, message.Body);
            return true; // Mark true so malformed messages don't block the queue
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId}", message.MessageId);
            return false;
        }
    }
}
