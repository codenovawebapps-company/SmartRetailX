using System.Collections.Concurrent;
using InventoryService.Models;

namespace InventoryService.Services;

public class InventoryService
{
    private readonly ConcurrentDictionary<string, int> _inventory = new();
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ILogger<InventoryService> logger)
    {
        _logger = logger;
        // Default seed stock
        _inventory["PROD001"] = 100;
        _inventory["PROD002"] = 50;
        _inventory["1"] = 100;
        _inventory["2"] = 75;
        _inventory["3"] = 50;
    }

    public bool CheckStock(string productId, int quantity)
    {
        return _inventory.TryGetValue(productId, out var stock) && stock >= quantity;
    }

    public bool ReduceStock(string productId, int quantity)
    {
        if (!CheckStock(productId, quantity))
        {
            _logger.LogWarning("Insufficient stock for ProductId: {ProductId}. Requested: {Quantity}, Available: {Available}", 
                productId, quantity, GetStock(productId));
            return false;
        }

        _inventory[productId] -= quantity;
        _logger.LogInformation("Stock reduced for ProductId: {ProductId} by {Quantity}. Remaining stock: {Remaining}", 
            productId, quantity, _inventory[productId]);
        return true;
    }

    public int GetStock(string productId)
    {
        return _inventory.TryGetValue(productId, out var stock) ? stock : 0;
    }

    public void SetStock(string productId, int quantity)
    {
        _inventory[productId] = quantity;
        _logger.LogInformation("Stock set for ProductId: {ProductId} to {Quantity}", productId, quantity);
    }

    public bool ProcessOrderCreatedEvent(OrderCreatedEvent orderEvent)
    {
        _logger.LogInformation("Processing OrderCreatedEvent for OrderId: {OrderId}, ProductId: {ProductId}, Quantity: {Quantity}",
            orderEvent.OrderId, orderEvent.ProductId, orderEvent.Quantity);

        return ReduceStock(orderEvent.ProductId, orderEvent.Quantity);
    }

    public IReadOnlyDictionary<string, int> GetAllStock()
    {
        return _inventory;
    }
}
