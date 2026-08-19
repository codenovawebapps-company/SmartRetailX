using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using System.Text.Json;

namespace OrderService.Services;

public class EventPublisher
{
    private readonly IAmazonEventBridge? _eventBridge;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventPublisher> _logger;
    private readonly string _eventBusName;

    public EventPublisher(
        IConfiguration configuration,
        ILogger<EventPublisher> logger,
        IAmazonEventBridge? eventBridge = null)
    {
        _configuration = configuration;
        _logger = logger;
        _eventBridge = eventBridge;
        _eventBusName = _configuration["EventBridge:EventBusName"] ?? "smartretailx-event-bus";
    }

    public async Task PublishEventAsync<T>(string detailType, T detailEvent)
    {
        var payload = JsonSerializer.Serialize(detailEvent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (_eventBridge == null)
        {
            _logger.LogInformation("[Local Simulation] Event published to {EventBus} with DetailType: {DetailType}. Payload: {Payload}",
                _eventBusName, detailType, payload);
            return;
        }

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
                        Detail = payload
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
            _logger.LogWarning(ex, "AWS EventBridge unavailable. Simulating local event broadcast for {DetailType}: {Payload}", detailType, payload);
        }
    }
}
