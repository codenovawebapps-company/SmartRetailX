using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using System.Text.Json;

namespace OrderService.Services;

public class EventPublisher
{
    private readonly IAmazonEventBridge _eventBridge;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventPublisher> _logger;
    private readonly string _eventBusName;

    public EventPublisher(
        IAmazonEventBridge eventBridge, 
        IConfiguration configuration,
        ILogger<EventPublisher> logger)
    {
        _eventBridge = eventBridge;
        _configuration = configuration;
        _logger = logger;
        _eventBusName = _configuration["EventBridge:EventBusName"] ?? "smartretailx-event-bus";
    }

    public async Task PublishEventAsync<T>(string detailType, T detailEvent)
    {
        try
        {
            var request = new PutEventsRequest
            {
                Entries = new List<PutEventsRequestEntry>
                {
                    new PutEventsRequestEntry
                    {
                        Source = "com.smartretailx.order-service",
                        EventBusName = _eventBusName,
                        DetailType = detailType,
                        Time = DateTime.UtcNow,
                        Detail = JsonSerializer.Serialize(detailEvent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                    }
                }
            };

            var response = await _eventBridge.PutEventsAsync(request);

            if (response.FailedEntryCount > 0)
            {
                _logger.LogError("Failed to publish event to EventBridge. DetailType: {DetailType}", detailType);
            }
            else
            {
                _logger.LogInformation("Successfully published event to EventBridge. DetailType: {DetailType}, EventId: {EventId}", detailType, response.Entries[0].EventId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while publishing event to EventBridge.");
        }
    }
}
