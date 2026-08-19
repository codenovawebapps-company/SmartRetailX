using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/v1")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly EventPublisher _eventPublisher;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        OrderDbContext db,
        EventPublisher eventPublisher,
        ILogger<OrdersController> logger)
    {
        _db = db;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1/orders
    /// Returns all orders.
    /// </summary>
    [HttpGet("orders")]
    public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// POST /api/v1/orders
    /// Creates and places a new order.
    /// </summary>
    [HttpPost("orders")]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
    {
        if (order.UserId <= 0 || order.Items == null || order.Items.Count == 0)
        {
            return BadRequest(new { message = "UserId and at least one order item are required." });
        }

        if (order.TotalAmount <= 0)
        {
            order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        }

        if (order.OrderDate == default)
        {
            order.OrderDate = DateTime.UtcNow;
        }

        if (string.IsNullOrWhiteSpace(order.Status))
        {
            order.Status = "Pending";
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Publish OrderCreated event asynchronously to EventBridge / SQS
        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var item in order.Items)
                {
                    await _eventPublisher.PublishEventAsync("OrderCreated", new
                    {
                        OrderId = order.Id,
                        UserId = order.UserId,
                        ProductId = item.ProductId.ToString(),
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        TotalAmount = order.TotalAmount,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreated event for Order #{OrderId}", order.Id);
            }
        });

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    /// <summary>
    /// GET /api/v1/orders/{id}
    /// Retrieves a single order by ID.
    /// </summary>
    [HttpGet("orders/{id}")]
    public async Task<ActionResult<Order>> GetOrderById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        return Ok(order);
    }

    /// <summary>
    /// GET /api/v1/orders/user/{userId}
    /// Standardized requirement: Retrieves all orders placed by a specific user.
    /// </summary>
    [HttpGet("orders/user/{userId}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByUserId(int userId)
    {
        var userOrders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return Ok(userOrders);
    }

    /// <summary>
    /// GET /api/v1/users/{id}/orders
    /// Backward-compatible alias for user orders.
    /// </summary>
    [HttpGet("users/{id}/orders")]
    public Task<ActionResult<IEnumerable<Order>>> GetOrdersByUserIdAlias(int id)
    {
        return GetOrdersByUserId(id);
    }

    /// <summary>
    /// PUT /api/v1/orders/{id}/status
    /// Updates the status of an existing order.
    /// </summary>
    [HttpPut("orders/{id}/status")]
    public async Task<ActionResult<Order>> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateRequest request)
    {
        var allowedStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

        if (string.IsNullOrWhiteSpace(request.Status) || !allowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = $"Invalid status '{request.Status}'. Allowed: {string.Join(", ", allowedStatuses)}"
            });
        }

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        order.Status = request.Status;
        await _db.SaveChangesAsync();

        return Ok(order);
    }

    /// <summary>
    /// DELETE /api/v1/orders/{id}
    /// Cancels / Deletes an order.
    /// </summary>
    [HttpDelete("orders/{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Order with ID {id} successfully deleted." });
    }
}
