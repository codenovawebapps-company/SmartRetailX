namespace OrderService.Models;

/// <summary>
/// Request body for PUT /api/v1/orders/{id}/status
/// </summary>
public class OrderStatusUpdateRequest
{
    /// <summary>
    /// New status. Allowed: Pending, Processing, Shipped, Delivered, Cancelled
    /// </summary>
    public string Status { get; set; } = "";
}
