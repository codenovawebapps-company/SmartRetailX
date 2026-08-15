namespace NotificationService.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? OrderId { get; set; }
    public string Type { get; set; } = ""; // ORDER_CREATED, ORDER_CONFIRMED, PAYMENT_SUCCESS, PAYMENT_FAILED, ORDER_CANCELLED
    public string Message { get; set; } = "";
    public string Status { get; set; } = "Sent"; // Sent, Failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
