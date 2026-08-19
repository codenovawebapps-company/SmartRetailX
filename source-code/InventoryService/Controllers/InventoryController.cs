using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryService.Data;
using InventoryService.Models;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/inventory")]
[Route("api/v1/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        InventoryDbContext db,
        ILogger<InventoryController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1/inventory
    /// Returns all product inventory stocks.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAllInventory()
    {
        var items = await _db.Items.ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// GET /api/v1/inventory/{productId}
    /// Returns the stock level for a product.
    /// </summary>
    [HttpGet("{productId}")]
    public async Task<IActionResult> GetInventory(string productId)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.ProductId == productId);
        var stock = item?.AvailableStock ?? 0;

        return Ok(new
        {
            productId,
            stock,
            reserved = item?.ReservedStock ?? 0,
            updatedAt = item?.UpdatedAt ?? DateTime.UtcNow
        });
    }

    /// <summary>
    /// PUT /api/v1/inventory/{productId}
    /// Updates or sets the stock level for a specific product directly.
    /// </summary>
    [HttpPut("{productId}")]
    public async Task<IActionResult> UpdateInventory(string productId, [FromBody] UpdateStockRequest request)
    {
        if (request.Stock < 0)
        {
            return BadRequest(new { message = "Stock level cannot be negative." });
        }

        var item = await _db.Items.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (item == null)
        {
            item = new InventoryItem
            {
                ProductId = productId,
                ProductName = $"Product {productId}",
                AvailableStock = request.Stock,
                ReservedStock = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Items.Add(item);
        }
        else
        {
            item.AvailableStock = request.Stock;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            productId = item.ProductId,
            stock = item.AvailableStock,
            message = $"Stock updated successfully for Product {productId}."
        });
    }

    /// <summary>
    /// GET /api/v1/inventory/check/{productId}
    /// Checks whether the requested quantity is in stock.
    /// </summary>
    [HttpGet("check/{productId}")]
    public async Task<IActionResult> CheckStock(string productId, [FromQuery] int quantity = 1)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.ProductId == productId);
        var currentStock = item?.AvailableStock ?? 0;
        var isAvailable = currentStock >= quantity;

        return Ok(new
        {
            productId,
            requestedQuantity = quantity,
            available = isAvailable,
            currentStock
        });
    }

    /// <summary>
    /// POST /api/v1/inventory/reduce
    /// Reduces stock for a product upon purchase.
    /// </summary>
    [HttpPost("reduce")]
    public async Task<IActionResult> ReduceStock([FromBody] StockActionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductId) || request.Quantity <= 0)
        {
            return BadRequest(new { message = "Invalid request payload. ProductId and Quantity (> 0) are required." });
        }

        var item = await _db.Items.FirstOrDefaultAsync(i => i.ProductId == request.ProductId);
        if (item == null || item.AvailableStock < request.Quantity)
        {
            return BadRequest(new
            {
                success = false,
                message = "Insufficient stock or product not found.",
                productId = request.ProductId,
                requestedQuantity = request.Quantity,
                currentStock = item?.AvailableStock ?? 0
            });
        }

        item.AvailableStock -= request.Quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Reduced stock for Product {ProductId} by {Quantity}. Remaining: {Remaining}",
            request.ProductId, request.Quantity, item.AvailableStock);

        return Ok(new
        {
            success = true,
            message = "Stock reduced successfully.",
            productId = request.ProductId,
            remainingStock = item.AvailableStock
        });
    }

    /// <summary>
    /// POST /api/v1/inventory/events/order-created
    /// Consumes and processes OrderCreated events asynchronously.
    /// </summary>
    [HttpPost("events/order-created")]
    public async Task<IActionResult> HandleOrderCreatedEvent([FromBody] OrderCreatedEvent orderEvent)
    {
        if (orderEvent == null || string.IsNullOrWhiteSpace(orderEvent.ProductId) || orderEvent.Quantity <= 0)
        {
            return BadRequest(new { message = "Invalid OrderCreatedEvent payload." });
        }

        var item = await _db.Items.FirstOrDefaultAsync(i => i.ProductId == orderEvent.ProductId);
        if (item == null || item.AvailableStock < orderEvent.Quantity)
        {
            _logger.LogWarning("Event stock reduction failed for Order {OrderId}: Insufficient stock for {ProductId}",
                orderEvent.OrderId, orderEvent.ProductId);

            return BadRequest(new
            {
                success = false,
                message = $"Stock reduction failed for Order {orderEvent.OrderId} due to insufficient stock.",
                orderId = orderEvent.OrderId,
                productId = orderEvent.ProductId
            });
        }

        item.AvailableStock -= orderEvent.Quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Processed OrderCreated event: Reduced stock for Order {OrderId}, Product {ProductId} by {Quantity}. Remaining: {Remaining}",
            orderEvent.OrderId, orderEvent.ProductId, orderEvent.Quantity, item.AvailableStock);

        return Ok(new
        {
            success = true,
            message = $"Stock reduced successfully for Order {orderEvent.OrderId}.",
            orderId = orderEvent.OrderId,
            productId = orderEvent.ProductId,
            remainingStock = item.AvailableStock
        });
    }
}
