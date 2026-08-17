using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
[Route("api/v1/notifications")]
public class NotificationController : ControllerBase
{
    private readonly NotificationService.Services.NotificationService _service;
    private readonly NotificationStore _notificationStore;
    private readonly MockNotificationSender _notificationSender;
    private readonly EventProcessor _eventProcessor;

    public NotificationController(
        NotificationService.Services.NotificationService service,
        NotificationStore notificationStore,
        MockNotificationSender notificationSender,
        EventProcessor eventProcessor)
    {
        _service = service;
        _notificationStore = notificationStore;
        _notificationSender = notificationSender;
        _eventProcessor = eventProcessor;
    }

    [HttpPost]
    public IActionResult Send([FromBody] NotificationEvent request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Invalid notification payload." });
        }

        _service.ProcessNotification(
            request.UserId,
            request.OrderId,
            request.Message);

        return Ok(new
        {
            message = "Notification processed successfully",
            eventType = request.EventType,
            userId = request.UserId,
            orderId = request.OrderId
        });
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

    [HttpPost("simulate-sqs")]
    public async Task<ActionResult> SimulateSqsEvent([FromBody] NotificationService.Models.Events.SqsEnvelope envelope)
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
        return StatusCode(500, new { message = "Failed to process event envelope." });
    }

    [HttpPost("clear-cache")]
    public ActionResult ClearIdempotencyCache()
    {
        _eventProcessor.ClearCache();
        return Ok(new { message = "Idempotency cache cleared." });
    }
}
