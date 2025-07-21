using Microsoft.AspNetCore.Mvc;
using Forma.Chains.Abstractions;
using Forma.Examples.Web.AspNetCore.Models;

namespace Forma.Examples.Web.AspNetCore.Controllers;

/// <summary>
/// Orders controller demonstrating Forma Chains pattern in ASP.NET Core
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IChainInvoker<OrderProcessingRequest, OrderProcessingResponse> _orderChain;
    private readonly IChainInvoker<PaymentProcessingRequest> _paymentChain;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IChainInvoker<OrderProcessingRequest, OrderProcessingResponse> orderChain,
        IChainInvoker<PaymentProcessingRequest> paymentChain,
        ILogger<OrdersController> logger)
    {
        _orderChain = orderChain;
        _paymentChain = paymentChain;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order using the order processing chain
    /// </summary>
    /// <param name="request">Order creation request</param>
    /// <returns>Created order details</returns>
    [HttpPost]
    public async Task<ActionResult<OrderProcessingResponse>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var processingRequest = new OrderProcessingRequest
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CustomerId = request.CustomerId,
                CustomerEmail = request.CustomerEmail,
                RequestId = Guid.NewGuid().ToString()
            };

            _logger.LogInformation("Processing order creation for customer {CustomerId}", request.CustomerId);
            
            var response = await _orderChain.HandleAsync(processingRequest);
            
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid order request: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Order processing failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during order creation");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Process payment using the payment processing chain
    /// </summary>
    /// <param name="orderId">Order ID to process payment for</param>
    /// <param name="request">Payment processing request</param>
    /// <returns>Payment processing result</returns>
    [HttpPost("{orderId}/payment")]
    public async Task<ActionResult> ProcessPayment(string orderId, [FromBody] ProcessPaymentRequest request)
    {
        try
        {
            var processingRequest = new PaymentProcessingRequest
            {
                OrderId = orderId,
                Amount = request.Amount,
                CardNumber = request.CardNumber,
                CustomerEmail = request.CustomerEmail,
                RequestId = Guid.NewGuid().ToString()
            };

            _logger.LogInformation("Processing payment for order {OrderId}", orderId);
            
            await _paymentChain.HandleAsync(processingRequest);
            
            return Ok(new { message = "Payment processed successfully", orderId });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid payment request: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Payment processing failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during payment processing");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get sample data for testing chains
    /// </summary>
    /// <returns>Sample data that can be used to test the chains</returns>
    [HttpGet("samples")]
    public ActionResult GetSampleData()
    {
        return Ok(new
        {
            orderSample = new CreateOrderRequest
            {
                ProductId = "PROD-001",
                Quantity = 2,
                CustomerId = "CUST-123",
                CustomerEmail = "customer@example.com"
            },
            paymentSample = new ProcessPaymentRequest
            {
                Amount = 99.99m,
                CardNumber = "4532-1234-5678-9012",
                CustomerEmail = "customer@example.com"
            }
        });
    }
}