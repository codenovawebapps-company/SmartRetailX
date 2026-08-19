namespace OrderService.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // Pending | Processing | Shipped | Delivered | Cancelled
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
