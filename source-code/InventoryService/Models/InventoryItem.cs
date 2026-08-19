namespace InventoryService.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int AvailableStock { get; set; }
    public int ReservedStock { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class UpdateStockRequest
{
    public int Stock { get; set; }
}

public class StockActionRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
