using Microsoft.AspNetCore.Mvc;
using InventoryService.Models;
using InventoryService.Services;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/inventory")]
[Route("api/v1/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService.Services.InventoryService _inventory;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        InventoryService.Services.InventoryService inventory,
        ILogger<InventoryController> logger)
    {
        _inventory = inventory;
        _logger = logger;
    }

    /// <summary>
    /// Get all product inventory stocks
    /// </summary>
    [HttpGet]
    public IActionResult GetAllInventory()
    {
        return Ok(_inventory.GetAllStock());
    }

    /// <summary>
    /// Get stock level for a product
    /// e.g. GET /api/inventory/PROD001
    /// </summary>
    [HttpGet("{productId}")]
    public IActionResult GetInventory(string productId)
    {
        var stock = _inventory.GetStock(productId);

        return Ok(new
        {
            productId,
            stock
        });
    }

    /// <summary>
    /// Check if stock is available
    /// </summary>
    [HttpGet("check/{productId}")]
    public IActionResult CheckStock(string productId, [FromQuery] int quantity = 1)
    {
        var isAvailable = _inventory.CheckStock(productId, quantity);
        var currentStock = _inventory.GetStock(productId);

        return Ok(new
        {
            productId,
            requestedQuantity = quantity,
            available = isAvailable,
            currentStock
        });
    }

    /// <summary>
    /// Reduce stock for a product
    /// </summary>
    [HttpPost("reduce")]
    public IActionResult ReduceStock([FromBody] StockActionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductId) || request.Quantity <= 0)
        {
            return BadRequest(new { message = "Invalid request payload. ProductId and Quantity (> 0) are required." });
        }

        var success = _inventory.ReduceStock(request.ProductId, request.Quantity);
        if (!success)
        {
            return BadRequest(new
            {
                success = false,
                message = "Insufficient stock or product not found.",
                productId = request.ProductId,
                requestedQuantity = request.Quantity,
                currentStock = _inventory.GetStock(request.ProductId)
            });
        }

        return Ok(new
        {
            success = true,
            message = "Stock reduced successfully.",
            productId = request.ProductId,
            remainingStock = _inventory.GetStock(request.ProductId)
        });
    }

    /// <summary>
    /// Consume/process OrderCreated event
    /// </summary>
    [HttpPost("events/order-created")]
    public IActionResult HandleOrderCreatedEvent([FromBody] OrderCreatedEvent orderEvent)
    {
        if (orderEvent == null || string.IsNullOrWhiteSpace(orderEvent.ProductId) || orderEvent.Quantity <= 0)
        {
            return BadRequest(new { message = "Invalid OrderCreatedEvent payload." });
        }

        var result = _inventory.ProcessOrderCreatedEvent(orderEvent);
        if (!result)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Stock reduction failed for Order {orderEvent.OrderId} due to insufficient stock.",
                orderId = orderEvent.OrderId,
                productId = orderEvent.ProductId
            });
        }

        return Ok(new
        {
            success = true,
            message = $"Stock reduced successfully for Order {orderEvent.OrderId}.",
            orderId = orderEvent.OrderId,
            productId = orderEvent.ProductId,
            remainingStock = _inventory.GetStock(orderEvent.ProductId)
        });
    }
}

public class StockActionRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
