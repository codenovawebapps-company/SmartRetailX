namespace PaymentService.Models.Events;

public class PaymentCompletedEvent
{
    public string Version { get; set; } = "1.0";
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = "Card";
    public string TransactionRef { get; set; } = "";
    public string PaidAt { get; set; } = "";
}
