using Forma.Chains.Abstractions;
using Forma.Chains.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Forma.Examples.Console.Chains;

// Program entry point demonstrating Forma Chains (Pipeline) pattern
public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection container
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Configure chains using Forma
        services.AddChain<PaymentRequest>(
            typeof(ValidationHandler), 
            typeof(FraudDetectionHandler), 
            typeof(PaymentProcessingHandler), 
            typeof(NotificationHandler));

        services.AddChain<OrderRequest, OrderResponse>(
            typeof(OrderValidationHandler), 
            typeof(InventoryCheckHandler), 
            typeof(PricingHandler), 
            typeof(OrderCreationHandler));

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        System.Console.WriteLine("=== Forma Chains (Pipeline) Pattern Example ===\n");
        System.Console.WriteLine("Press key to continue...");
        System.Console.ReadKey();

        // Example 1: Payment processing pipeline (no response)
        System.Console.WriteLine("1. Processing payment through chain...");
        var paymentChain = serviceProvider.GetRequiredService<IChainInvoker<PaymentRequest>>();
        
        var paymentRequest = new PaymentRequest
        {
            Amount = 100.50m,
            CardNumber = "4532-1234-5678-9012",
            CustomerEmail = "john@example.com",
            Results = new List<string>()
        };
        
        await paymentChain.HandleAsync(paymentRequest);
        
        System.Console.WriteLine("Payment processing completed. Steps executed:");
        System.Console.WriteLine("Press key to continue...");
        System.Console.ReadKey();

        foreach (var step in paymentRequest.Results)
        {
            System.Console.WriteLine($"  ✓ {step}");
        }
        System.Console.WriteLine();

        // Example 2: Order processing pipeline (with response)
        System.Console.WriteLine("2. Processing order through chain...");
        var orderChain = serviceProvider.GetRequiredService<IChainInvoker<OrderRequest, OrderResponse>>();
        
        var orderRequest = new OrderRequest
        {
            ProductId = "PROD-001",
            Quantity = 2,
            CustomerId = "CUST-123",
            Results = new List<string>()
        };
        
        var orderResponse = await orderChain.HandleAsync(orderRequest);
        
        System.Console.WriteLine("Order processing completed. Steps executed:");
        System.Console.WriteLine("Press key to continue...");
        System.Console.ReadKey();

        foreach (var step in orderRequest.Results)
        {
            System.Console.WriteLine($"  ✓ {step}");
        }
        System.Console.WriteLine($"Order Result: ID={orderResponse?.OrderId}, Total=${orderResponse?.TotalAmount:F2}");
        System.Console.WriteLine();

        // Example 3: Failed payment processing (fraud detection)
        System.Console.WriteLine("3. Testing fraud detection in payment chain...");
        var fraudulentPayment = new PaymentRequest
        {
            Amount = 10000m, // High amount triggers fraud detection
            CardNumber = "1234-5678-9012-3456",
            CustomerEmail = "suspicious@example.com",
            Results = new List<string>()
        };
        
        try
        {
            await paymentChain.HandleAsync(fraudulentPayment);
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine($"Payment blocked: {ex.Message}");
            System.Console.WriteLine("Steps executed before failure:");
            foreach (var step in fraudulentPayment.Results)
            {
                System.Console.WriteLine($"  ✓ {step}");
            }
        }

        System.Console.WriteLine("\n=== Example completed ===");
    }
}

// Request models
public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public List<string> Results { get; set; } = new();
}

public class OrderRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public List<string> Results { get; set; } = new();
}

public class OrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Payment processing chain handlers
public class ValidationHandler : IChainHandler<PaymentRequest>
{
    private readonly ILogger<ValidationHandler> _logger;

    public ValidationHandler(ILogger<ValidationHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task HandleAsync(PaymentRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating payment request");
        
        // Simulate validation logic
        await Task.Delay(50, cancellationToken);
        
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");
            
        if (string.IsNullOrWhiteSpace(request.CardNumber))
            throw new ArgumentException("Card number is required");
            
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            throw new ArgumentException("Customer email is required");

        request.Results.Add("Payment validated");
        System.Console.WriteLine("   [ValidationHandler] Payment request validated successfully");
        
        await next(cancellationToken);
    }
}

public class FraudDetectionHandler : IChainHandler<PaymentRequest>
{
    private readonly ILogger<FraudDetectionHandler> _logger;

    public FraudDetectionHandler(ILogger<FraudDetectionHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task HandleAsync(PaymentRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running fraud detection");
        
        // Simulate fraud detection logic
        await Task.Delay(100, cancellationToken);
        
        // Simple fraud detection: high amounts are suspicious
        if (request.Amount > 5000m)
        {
            request.Results.Add("Fraud detected - processing stopped");
            throw new InvalidOperationException("Potential fraud detected - transaction blocked");
        }
        
        request.Results.Add("Fraud check passed");
        System.Console.WriteLine("   [FraudDetectionHandler] Fraud check completed - transaction approved");
        
        await next(cancellationToken);
    }
}

public class PaymentProcessingHandler : IChainHandler<PaymentRequest>
{
    private readonly ILogger<PaymentProcessingHandler> _logger;

    public PaymentProcessingHandler(ILogger<PaymentProcessingHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task HandleAsync(PaymentRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payment");
        
        // Simulate payment processing
        await Task.Delay(200, cancellationToken);
        
        request.Results.Add("Payment processed");
        System.Console.WriteLine($"   [PaymentProcessingHandler] Payment of ${request.Amount:F2} processed successfully");
        
        await next(cancellationToken);
    }
}

public class NotificationHandler : IChainHandler<PaymentRequest>
{
    private readonly ILogger<NotificationHandler> _logger;

    public NotificationHandler(ILogger<NotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task HandleAsync(PaymentRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending payment notification");
        
        // Simulate notification sending
        await Task.Delay(75, cancellationToken);
        
        request.Results.Add("Customer notified");
        System.Console.WriteLine($"   [NotificationHandler] Payment confirmation sent to {request.CustomerEmail}");
        
        await next(cancellationToken);
    }
}

// Order processing chain handlers (with response)
public class OrderValidationHandler : IChainHandler<OrderRequest, OrderResponse>
{
    private readonly ILogger<OrderValidationHandler> _logger;

    public OrderValidationHandler(ILogger<OrderValidationHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<OrderResponse> HandleAsync(OrderRequest request, Func<CancellationToken, Task<OrderResponse?>> next, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating order request");
        
        await Task.Delay(30, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(request.ProductId))
            throw new ArgumentException("Product ID is required");
            
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");

        request.Results.Add("Order validated");
        System.Console.WriteLine("   [OrderValidationHandler] Order request validated");
        
        var response = await next(cancellationToken);
        return response ?? new OrderResponse();
    }
}

public class InventoryCheckHandler : IChainHandler<OrderRequest, OrderResponse>
{
    private readonly ILogger<InventoryCheckHandler> _logger;

    public InventoryCheckHandler(ILogger<InventoryCheckHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<OrderResponse> HandleAsync(OrderRequest request, Func<CancellationToken, Task<OrderResponse?>> next, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking inventory");
        
        await Task.Delay(80, cancellationToken);
        
        // Simulate inventory check
        var availableStock = 10; // Simulated available stock
        if (request.Quantity > availableStock)
            throw new InvalidOperationException($"Insufficient stock. Available: {availableStock}, Requested: {request.Quantity}");

        request.Results.Add("Inventory checked");
        System.Console.WriteLine($"   [InventoryCheckHandler] Inventory available for {request.Quantity} units of {request.ProductId}");
        
        var response = await next(cancellationToken);
        return response ?? new OrderResponse();
    }
}

public class PricingHandler : IChainHandler<OrderRequest, OrderResponse>
{
    private readonly ILogger<PricingHandler> _logger;

    public PricingHandler(ILogger<PricingHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<OrderResponse> HandleAsync(OrderRequest request, Func<CancellationToken, Task<OrderResponse?>> next, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating pricing");
        
        await Task.Delay(60, cancellationToken);
        
        // Simulate pricing calculation
        var unitPrice = 25.99m; // Simulated unit price
        var totalAmount = unitPrice * request.Quantity;

        request.Results.Add("Pricing calculated");
        System.Console.WriteLine($"   [PricingHandler] Total amount calculated: ${totalAmount:F2}");
        
        var response = await next(cancellationToken);
        if (response != null)
        {
            response.TotalAmount = totalAmount;
        }
        
        return response ?? new OrderResponse { TotalAmount = totalAmount };
    }
}

public class OrderCreationHandler : IChainHandler<OrderRequest, OrderResponse>
{
    private readonly ILogger<OrderCreationHandler> _logger;

    public OrderCreationHandler(ILogger<OrderCreationHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<OrderResponse> HandleAsync(OrderRequest request, Func<CancellationToken, Task<OrderResponse?>> next, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order");
        
        await Task.Delay(120, cancellationToken);
        
        // Create the order (final step in chain)
        var orderId = $"ORD-{Random.Shared.Next(1000, 9999)}";
        
        request.Results.Add("Order created");
        System.Console.WriteLine($"   [OrderCreationHandler] Order {orderId} created successfully");
        
        // This is the final handler, so we don't call next()
        return new OrderResponse
        {
            OrderId = orderId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
