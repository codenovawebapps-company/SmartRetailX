using NotificationService.Models;
using NotificationService.Models.Events;
using System.Collections.Concurrent;
using System.Text.Json;

namespace NotificationService.Services;

public class EventProcessor
{
    private readonly NotificationStore _notificationStore;
    private readonly MockNotificationSender _notificationSender;
    private readonly ILogger<EventProcessor> _logger;
    private static readonly ConcurrentDictionary<string, byte> _processedEventIds = new();

    public EventProcessor(
        NotificationStore notificationStore,
        MockNotificationSender notificationSender,
        ILogger<EventProcessor> logger)
    {
        _notificationStore = notificationStore;
        _notificationSender = notificationSender;
        _logger = logger;
    }

    public async Task<bool> ProcessEnvelopeAsync(SqsEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.Id))
        {
            _logger.LogWarning("Event ID is missing from envelope. Processing as non-idempotent.");
        }
        else if (_processedEventIds.ContainsKey(envelope.Id))
        {
            _logger.LogInformation("Duplicate event skipped (Idempotency check). Event ID: {EventId}", envelope.Id);
            return true; // Already processed
        }

        bool result = await RouteEventAsync(envelope);

        if (result && !string.IsNullOrWhiteSpace(envelope.Id))
        {
            _processedEventIds.TryAdd(envelope.Id, 0);
        }

        return result;
    }

    public void ClearCache()
    {
        _processedEventIds.Clear();
    }

    private async Task<bool> RouteEventAsync(SqsEnvelope envelope)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _logger.LogInformation("Processing event: {EventId}, Type: {DetailType}, Source: {Source}", 
            envelope.Id, envelope.DetailType, envelope.Source);

        switch (envelope.DetailType)
        {
            case "OrderCreated":
                var orderCreated = JsonSerializer.Deserialize<OrderCreatedEvent>(envelope.Detail.GetRawText(), options);
                if (orderCreated != null)
                {
                    return await HandleOrderCreatedAsync(orderCreated);
                }
                break;

            case "PaymentCompleted":
                var paymentCompleted = JsonSerializer.Deserialize<PaymentCompletedEvent>(envelope.Detail.GetRawText(), options);
                if (paymentCompleted != null)
                {
                    return await HandlePaymentCompletedAsync(paymentCompleted);
                }
                break;

            case "PaymentFailed":
                var paymentFailed = JsonSerializer.Deserialize<PaymentFailedEvent>(envelope.Detail.GetRawText(), options);
                if (paymentFailed != null)
                {
                    return await HandlePaymentFailedAsync(paymentFailed);
                }
                break;

            case "OrderStatusChanged":
                var orderStatusChanged = JsonSerializer.Deserialize<OrderStatusChangedEvent>(envelope.Detail.GetRawText(), options);
                if (orderStatusChanged != null)
                {
                    return await HandleOrderStatusChangedAsync(orderStatusChanged);
                }
                break;

            default:
                _logger.LogWarning("Unknown or unhandled event detail type: {DetailType}", envelope.DetailType);
                return true; // Delete unsupported event
        }

        return false;
    }

    private async Task<bool> HandleOrderCreatedAsync(OrderCreatedEvent ev)
    {
        _logger.LogInformation("Handling OrderCreatedEvent for Order ID: {OrderId}", ev.OrderId);

        var notification = new Notification
        {
            UserId = ev.UserId,
            OrderId = ev.OrderId,
            Type = "ORDER_CREATED",
            Message = $"Order #{ev.OrderId} has been created successfully. Total: {ev.TotalAmount:C} {ev.Currency}.",
            CreatedAt = DateTime.UtcNow
        };

        _notificationStore.Add(notification);
        return await _notificationSender.SendNotificationAsync(notification);
    }

    private async Task<bool> HandlePaymentCompletedAsync(PaymentCompletedEvent ev)
    {
        _logger.LogInformation("Handling PaymentCompletedEvent for Order ID: {OrderId}", ev.OrderId);

        var notification = new Notification
        {
            UserId = ev.UserId,
            OrderId = ev.OrderId,
            Type = "PAYMENT_SUCCESS",
            Message = $"Payment confirmed for Order #{ev.OrderId}. Amount Charged: {ev.Amount:C} {ev.Currency}. Transaction Reference: {ev.TransactionRef}.",
            CreatedAt = DateTime.UtcNow
        };

        _notificationStore.Add(notification);
        return await _notificationSender.SendNotificationAsync(notification);
    }

    private async Task<bool> HandlePaymentFailedAsync(PaymentFailedEvent ev)
    {
        _logger.LogInformation("Handling PaymentFailedEvent for Order ID: {OrderId}", ev.OrderId);

        var notification = new Notification
        {
            UserId = ev.UserId,
            OrderId = ev.OrderId,
            Type = "PAYMENT_FAILED",
            Message = $"Payment attempt failed for Order #{ev.OrderId}. Reason: {ev.FailureReason}.",
            CreatedAt = DateTime.UtcNow
        };

        _notificationStore.Add(notification);
        return await _notificationSender.SendNotificationAsync(notification);
    }

    private async Task<bool> HandleOrderStatusChangedAsync(OrderStatusChangedEvent ev)
    {
        _logger.LogInformation("Handling OrderStatusChangedEvent for Order ID: {OrderId}", ev.OrderId);

        string notificationType;
        string message;

        switch (ev.StatusTo.ToLowerInvariant())
        {
            case "processing":
                notificationType = "ORDER_CONFIRMED";
                message = $"Order #{ev.OrderId} is now confirmed and is being processed.";
                break;
            case "cancelled":
                notificationType = "ORDER_CANCELLED";
                message = $"Order #{ev.OrderId} has been cancelled. Reason: {ev.Reason}.";
                break;
            default:
                notificationType = "ORDER_STATUS_CHANGED";
                message = $"Order #{ev.OrderId} status has been updated to {ev.StatusTo}.";
                break;
        }

        var notification = new Notification
        {
            UserId = ev.UserId,
            OrderId = ev.OrderId,
            Type = notificationType,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        _notificationStore.Add(notification);
        return await _notificationSender.SendNotificationAsync(notification);
    }
}
