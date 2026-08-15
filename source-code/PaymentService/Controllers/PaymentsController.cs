using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using PaymentService.Models.Events;
using PaymentService.Services;
using System.Collections.Concurrent;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private static readonly ConcurrentDictionary<int, Payment> _payments = new();
    private static int _nextId = 0;
    
    private readonly EventPublisher _eventPublisher;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(EventPublisher eventPublisher, ILogger<PaymentsController> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] PaymentRequest request)
    {
        if (request.OrderId <= 0 || request.UserId <= 0)
        {
            return BadRequest(new { message = "Invalid payment request. OrderId and UserId are required." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Payment amount must be greater than zero." });
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            return BadRequest(new { message = "Currency is required." });
        }

        var payment = new Payment
        {
            Id = Interlocked.Increment(ref _nextId),
            OrderId = request.OrderId,
            UserId = request.UserId,
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethod = request.PaymentMethod,
            CreatedAt = DateTime.UtcNow
        };

        // Simulate payment gateway behavior
        // We trigger failure if amount is exactly 99.99 (or 9999) or if paymentMethod is "FAIL"
        if (request.Amount == 99.99m || request.PaymentMethod.Equals("FAIL", StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = "Failed";
            payment.FailureReason = "insufficient_funds";
            _payments[payment.Id] = payment;

            _logger.LogWarning("Payment failed for Order: {OrderId}, Payment ID: {PaymentId}", request.OrderId, payment.Id);

            // Publish PaymentFailed event
            var failedEvent = new PaymentFailedEvent
            {
                Version = "1.0",
                PaymentId = payment.Id,
                OrderId = payment.OrderId,
                UserId = payment.UserId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                FailureReason = payment.FailureReason,
                FailedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
            await _eventPublisher.PublishEventAsync("PaymentFailed", failedEvent);

            return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, payment);
        }
        else
        {
            payment.Status = "Success";
            payment.TransactionRef = "txn_" + Guid.NewGuid().ToString("n")[..12];
            payment.PaidAt = DateTime.UtcNow;
            _payments[payment.Id] = payment;

            _logger.LogInformation("Payment succeeded for Order: {OrderId}, Payment ID: {PaymentId}, Ref: {Ref}", 
                request.OrderId, payment.Id, payment.TransactionRef);

            // Publish PaymentCompleted event
            var completedEvent = new PaymentCompletedEvent
            {
                Version = "1.0",
                PaymentId = payment.Id,
                OrderId = payment.OrderId,
                UserId = payment.UserId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentMethod = payment.PaymentMethod,
                TransactionRef = payment.TransactionRef,
                PaidAt = payment.PaidAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
            await _eventPublisher.PublishEventAsync("PaymentCompleted", completedEvent);

            return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, payment);
        }
    }

    [HttpGet]
    public ActionResult<IEnumerable<Payment>> GetPayments()
    {
        return Ok(_payments.Values);
    }

    [HttpGet("{id}")]
    public ActionResult<Payment> GetPaymentById(int id)
    {
        if (_payments.TryGetValue(id, out var payment))
        {
            return Ok(payment);
        }
        return NotFound(new { message = $"Payment with ID {id} not found." });
    }

    [HttpGet("order/{orderId}")]
    public ActionResult<IEnumerable<Payment>> GetPaymentsByOrderId(int orderId)
    {
        var orderPayments = _payments.Values.Where(p => p.OrderId == orderId).ToList();
        return Ok(orderPayments);
    }
}
