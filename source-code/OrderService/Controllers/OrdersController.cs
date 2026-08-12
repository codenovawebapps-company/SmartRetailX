using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using System.Collections.Concurrent;

namespace OrderService.Controllers;

[ApiController]
[Route("api/v1")]
public class OrdersController : ControllerBase
{
    private static readonly ConcurrentDictionary<int, Order> _orders = new();
    private static int _nextId = 0;

    [HttpPost("orders")]
    public ActionResult<Order> CreateOrder([FromBody] Order order)
    {
        if (order.Id <= 0)
        {
            order.Id = Interlocked.Increment(ref _nextId);
        }
        else
        {
            int currentId;
            do
            {
                currentId = _nextId;
                if (order.Id < currentId) break;
            } while (Interlocked.CompareExchange(ref _nextId, order.Id + 1, currentId) != currentId);
        }

        if (order.TotalAmount <= 0 && order.Items.Count > 0)
        {
            order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        }

        if (order.OrderDate == default)
        {
            order.OrderDate = DateTime.UtcNow;
        }

        _orders[order.Id] = order;
        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    [HttpGet("orders/{id}")]
    public ActionResult<Order> GetOrderById(int id)
    {
        if (_orders.TryGetValue(id, out var order))
        {
            return Ok(order);
        }
        return NotFound(new { message = $"Order with ID {id} not found." });
    }

    [HttpGet("users/{id}/orders")]
    public ActionResult<IEnumerable<Order>> GetOrdersByUserId(int id)
    {
        var userOrders = _orders.Values.Where(o => o.UserId == id).ToList();
        return Ok(userOrders);
    }

    /// <summary>
    /// PUT /api/v1/orders/{id}/status
    /// Updates the status of an existing order.
    /// Allowed values: Pending, Processing, Shipped, Delivered, Cancelled
    /// </summary>
    [HttpPut("orders/{id}/status")]
    public ActionResult<Order> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateRequest request)
    {
        var allowedStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

        if (!allowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = $"Invalid status '{request.Status}'. Allowed: {string.Join(", ", allowedStatuses)}"
            });
        }

        if (!_orders.TryGetValue(id, out var order))
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        order.Status = request.Status;
        _orders[id] = order;
        return Ok(order);
    }
}
