namespace NotificationService.Models;

public class NotificationEvent
{
    public string EventType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
