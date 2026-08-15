using System.Text.Json.Serialization;

namespace NotificationService.Models.Events;

public class SqsEnvelope
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("detail-type")]
    public string DetailType { get; set; } = "";

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("time")]
    public string Time { get; set; } = "";

    [JsonPropertyName("detail")]
    public System.Text.Json.JsonElement Detail { get; set; }
}

public class OrderCreatedEvent
{
    public string Version { get; set; } = "";
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "";
    public string Status { get; set; } = "";
    public string OrderDate { get; set; } = "";
    public List<OrderCreatedItem> Items { get; set; } = new();
}

public class OrderCreatedItem
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PaymentCompletedEvent
{
    public string Version { get; set; } = "";
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string TransactionRef { get; set; } = "";
    public string PaidAt { get; set; } = "";
}

public class PaymentFailedEvent
{
    public string Version { get; set; } = "";
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public string FailureReason { get; set; } = "";
    public string FailedAt { get; set; } = "";
}

public class OrderStatusChangedEvent
{
    public string Version { get; set; } = "";
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string StatusFrom { get; set; } = "";
    public string StatusTo { get; set; } = "";
    public string Reason { get; set; } = "";
    public string ChangedAt { get; set; } = "";
}
