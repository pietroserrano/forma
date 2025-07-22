using Forma.Chains.Abstractions;
using Forma.Examples.Web.AspNetCore.Models;

namespace Forma.Examples.Web.AspNetCore.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var orders = endpoints.MapGroup("/api/orders")
            .WithTags("Orders")
            .WithOpenApi();

        // Create order
        orders.MapPost("/", async (
            CreateOrderRequest request,
            IChainInvoker<OrderProcessingRequest, OrderProcessingResponse> orderChain,
            ILogger<Program> logger) =>
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

                logger.LogInformation("Processing order creation for customer {CustomerId}", request.CustomerId);
                
                var response = await orderChain.HandleAsync(processingRequest);
                
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning("Invalid order request: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Order processing failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during order creation");
                return Results.Problem("An unexpected error occurred");
            }
        })
        .WithSummary("Create a new order using the order processing chain")
        .WithDescription("Creates a new order through the validation, inventory, pricing, and creation chain")
        .Accepts<CreateOrderRequest>("application/json")
        .Produces<OrderProcessingResponse>()
        .Produces(400);

        // Process payment
        orders.MapPost("/{orderId}/payment", async (
            string orderId,
            ProcessPaymentRequest request,
            IChainInvoker<PaymentProcessingRequest> paymentChain,
            ILogger<Program> logger) =>
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

                logger.LogInformation("Processing payment for order {OrderId}", orderId);
                
                await paymentChain.HandleAsync(processingRequest);
                
                return Results.Ok(new { message = "Payment processed successfully", orderId });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning("Invalid payment request: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Payment processing failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during payment processing");
                return Results.Problem("An unexpected error occurred");
            }
        })
        .WithSummary("Process payment using the payment processing chain")
        .WithDescription("Processes payment through the validation, fraud detection, payment, and notification chain")
        .Accepts<ProcessPaymentRequest>("application/json")
        .Produces<object>()
        .Produces(400);

        // Get sample data
        orders.MapGet("/samples", () =>
        {
            return Results.Ok(new
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
        })
        .WithSummary("Get sample data for testing chains")
        .WithDescription("Returns sample data that can be used to test the order and payment chains")
        .Produces<object>();
    }
}