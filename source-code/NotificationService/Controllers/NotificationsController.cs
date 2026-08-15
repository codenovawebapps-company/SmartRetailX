using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Models.Events;
using NotificationService.Services;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationStore _notificationStore;
    private readonly MockNotificationSender _notificationSender;
    private readonly EventProcessor _eventProcessor;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        NotificationStore notificationStore,
        MockNotificationSender notificationSender,
        EventProcessor eventProcessor,
        ILogger<NotificationsController> logger)
    {
        _notificationStore = notificationStore;
        _notificationSender = notificationSender;
        _eventProcessor = eventProcessor;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Notification>> GetNotifications()
    {
        return Ok(_notificationStore.Notifications.Values);
    }

    [HttpGet("{id}")]
    public ActionResult<Notification> GetNotificationById(int id)
    {
        if (_notificationStore.Notifications.TryGetValue(id, out var notification))
        {
            return Ok(notification);
        }
        return NotFound(new { message = $"Notification with ID {id} not found." });
    }

    [HttpPost]
    public async Task<ActionResult<Notification>> CreateNotification([FromBody] NotificationRequest request)
    {
        if (request.UserId <= 0)
        {
            return BadRequest(new { message = "UserId is required and must be greater than zero." });
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            return BadRequest(new { message = "Notification Type is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Notification Message is required." });
        }

        var notification = new Notification
        {
            UserId = request.UserId,
            OrderId = request.OrderId,
            Type = request.Type,
            Message = request.Message,
            CreatedAt = DateTime.UtcNow
        };

        _notificationStore.Add(notification);

        bool sent = await _notificationSender.SendNotificationAsync(notification);

        if (!sent)
        {
            return StatusCode(500, new { message = "Failed to send notification simulator message." });
        }

        return CreatedAtAction(nameof(GetNotificationById), new { id = notification.Id }, notification);
    }

    /// <summary>
    /// POST /api/v1/notifications/simulate-sqs
    /// Simulates SQS queue pulling an EventBridge envelope to test routing, parsing, and idempotency.
    /// </summary>
    [HttpPost("simulate-sqs")]
    public async Task<ActionResult> SimulateSqsEvent([FromBody] SqsEnvelope envelope)
    {
        if (envelope == null)
        {
            return BadRequest(new { message = "Invalid event envelope." });
        }

        bool result = await _eventProcessor.ProcessEnvelopeAsync(envelope);

        if (result)
        {
            return Ok(new { message = "Event processed successfully." });
        }
        else
        {
            return StatusCode(500, new { message = "Failed to process event envelope." });
        }
    }

    /// <summary>
    /// POST /api/v1/notifications/clear-cache
    /// Clears processed event IDs idempotency cache for test validation.
    /// </summary>
    [HttpPost("clear-cache")]
    public ActionResult ClearIdempotencyCache()
    {
        _eventProcessor.ClearCache();
        return Ok(new { message = "Idempotency cache cleared." });
    }
}
