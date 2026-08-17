namespace InventoryService.Models;

public class OrderCreatedEvent
{
    public string EventType { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
