namespace PaymentService.Models.Events;

public class PaymentFailedEvent
{
    public string Version { get; set; } = "1.0";
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string FailureReason { get; set; } = "";
    public string FailedAt { get; set; } = "";
}
