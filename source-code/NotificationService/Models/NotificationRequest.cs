namespace NotificationService.Models;

public class NotificationRequest
{
    public int UserId { get; set; }
    public int? OrderId { get; set; }
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
}
