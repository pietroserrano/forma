using Forma.Chains.Abstractions;
using Forma.Examples.Web.AspNetCore.Models;

namespace Forma.Examples.Web.AspNetCore.Chains;

/// <summary>
/// Validates the order processing request
/// </summary>
public class OrderValidationHandler : IChainHandler<OrderProcessingRequest, OrderProcessingResponse>
{
    private readonly ILogger<OrderValidationHandler> _logger;

    public OrderValidationHandler(ILogger<OrderValidationHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(OrderProcessingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<OrderProcessingResponse> HandleAsync(
        OrderProcessingRequest request,
        Func<CancellationToken, Task<OrderProcessingResponse?>> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating order request {RequestId}", request.RequestId);
        
        // Simulate validation delay
        await Task.Delay(50, cancellationToken);
        
        // Validate request
        if (string.IsNullOrWhiteSpace(request.ProductId))
            throw new ArgumentException("Product ID is required");
            
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
            
        if (string.IsNullOrWhiteSpace(request.CustomerId))
            throw new ArgumentException("Customer ID is required");
            
        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || !request.CustomerEmail.Contains('@'))
            throw new ArgumentException("Valid customer email is required");

        request.ProcessingSteps.Add("Order validation completed");
        _logger.LogInformation("Order validation passed for request {RequestId}", request.RequestId);
        
        var response = await next(cancellationToken);
        return response ?? new OrderProcessingResponse();
    }
}

/// <summary>
/// Checks inventory availability for the order
/// </summary>
public class InventoryCheckHandler : IChainHandler<OrderProcessingRequest, OrderProcessingResponse>
{
    private readonly ILogger<InventoryCheckHandler> _logger;

    public InventoryCheckHandler(ILogger<InventoryCheckHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(OrderProcessingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<OrderProcessingResponse> HandleAsync(
        OrderProcessingRequest request,
        Func<CancellationToken, Task<OrderProcessingResponse?>> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking inventory for product {ProductId}", request.ProductId);
        
        // Simulate inventory check delay
        await Task.Delay(100, cancellationToken);
        
        // Simulate inventory logic
        var availableStock = GetAvailableStock(request.ProductId);
        if (request.Quantity > availableStock)
        {
            throw new InvalidOperationException(
                $"Insufficient stock for product {request.ProductId}. Available: {availableStock}, Requested: {request.Quantity}");
        }

        request.ProcessingSteps.Add($"Inventory checked - {request.Quantity} units available");
        _logger.LogInformation("Inventory check passed for {Quantity} units of {ProductId}", 
            request.Quantity, request.ProductId);
        
        var response = await next(cancellationToken);
        return response ?? new OrderProcessingResponse();
    }

    private static int GetAvailableStock(string productId)
    {
        // Simulate different stock levels for different products
        return productId switch
        {
            "PROD-001" => 50,
            "PROD-002" => 25,
            "PROD-003" => 5,
            _ => 10
        };
    }
}

/// <summary>
/// Calculates pricing for the order
/// </summary>
public class OrderPricingHandler : IChainHandler<OrderProcessingRequest, OrderProcessingResponse>
{
    private readonly ILogger<OrderPricingHandler> _logger;

    public OrderPricingHandler(ILogger<OrderPricingHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(OrderProcessingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<OrderProcessingResponse> HandleAsync(
        OrderProcessingRequest request,
        Func<CancellationToken, Task<OrderProcessingResponse?>> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating pricing for order {RequestId}", request.RequestId);
        
        // Simulate pricing calculation delay
        await Task.Delay(75, cancellationToken);
        
        var unitPrice = GetUnitPrice(request.ProductId);
        var totalAmount = unitPrice * request.Quantity;
        
        // Apply discounts for bulk orders
        if (request.Quantity >= 10)
        {
            totalAmount *= 0.9m; // 10% discount
        }

        request.ProcessingSteps.Add($"Pricing calculated - Total: ${totalAmount:F2}");
        _logger.LogInformation("Pricing calculated for order {RequestId}: ${TotalAmount:F2}", 
            request.RequestId, totalAmount);
        
        var response = await next(cancellationToken);
        if (response != null)
        {
            response.TotalAmount = totalAmount;
        }
        
        return response ?? new OrderProcessingResponse { TotalAmount = totalAmount };
    }

    private static decimal GetUnitPrice(string productId)
    {
        // Simulate different prices for different products
        return productId switch
        {
            "PROD-001" => 25.99m,
            "PROD-002" => 45.50m,
            "PROD-003" => 199.99m,
            _ => 19.99m
        };
    }
}

/// <summary>
/// Creates the final order
/// </summary>
public class OrderCreationHandler : IChainHandler<OrderProcessingRequest, OrderProcessingResponse>
{
    private readonly ILogger<OrderCreationHandler> _logger;

    public OrderCreationHandler(ILogger<OrderCreationHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(OrderProcessingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<OrderProcessingResponse> HandleAsync(
        OrderProcessingRequest request,
        Func<CancellationToken, Task<OrderProcessingResponse?>> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for request {RequestId}", request.RequestId);
        
        // Simulate order creation delay
        await Task.Delay(150, cancellationToken);
        
        var orderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        
        request.ProcessingSteps.Add($"Order {orderId} created successfully");
        _logger.LogInformation("Order {OrderId} created for customer {CustomerId}", orderId, request.CustomerId);
        
        // This is the final handler, so we don't call next()
        return new OrderProcessingResponse
        {
            OrderId = orderId,
            CreatedAt = DateTime.UtcNow,
            ProcessingSteps = new List<string>(request.ProcessingSteps),
            Status = "Created"
        };
    }
}