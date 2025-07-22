using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator.Extensions;
using Forma.Decorator.Extensions;
using Forma.Chains.Extensions;
using Forma.Chains.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Forma.Examples.Console.DependencyInjection;

// Program entry point demonstrating Forma's complete integration with Dependency Injection
public class Program
{
    public static async Task Main(string[] args)
    {
        // Create a hosted application with proper DI setup
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Register application services
                RegisterApplicationServices(services);
                
                // Configure Forma patterns
                ConfigureFormaPatterns(services);
            })
            .Build();

        System.Console.WriteLine("=== Forma Complete Integration Example ===\n");

        // Get the application service and run the demo
        var app = host.Services.GetRequiredService<ECommerceApplication>();
        await app.RunDemoAsync();

        System.Console.WriteLine("\n=== Integration example completed ===");
        
        await host.StopAsync();
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        // Register core business services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPaymentService, PaymentService>();
        
        // Register the main application
        services.AddScoped<ECommerceApplication>();
        
        // Register command/query handlers for Mediator
        services.AddScoped<IHandler<CreateCustomerCommand>, CreateCustomerHandler>();
        services.AddScoped<IHandler<GetCustomerQuery, Customer>, GetCustomerHandler>();
        services.AddScoped<IHandler<ProcessOrderCommand, OrderResult>, ProcessOrderHandler>();
        
        // Register chain handlers
        services.AddScoped<OrderValidationChainHandler>();
        services.AddScoped<InventoryChainHandler>();
        services.AddScoped<PaymentChainHandler>();
        services.AddScoped<FulfillmentChainHandler>();
    }

    private static void ConfigureFormaPatterns(IServiceCollection services)
    {
        // 1. Configure Mediator pattern
        services.AddRequestMediator(config =>
        {
            config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
            config.AddRequestPreProcessor<LoggingPreProcessor>();
            config.AddRequestPostProcessor<MetricsPostProcessor>();
        });

        // 2. Configure Decorator pattern
        services.Decorate<IEmailService, RetryEmailDecorator>();
        services.Decorate<IEmailService, LoggingEmailDecorator>();
        services.Decorate<IPaymentService, SecurityPaymentDecorator>();
        services.Decorate<IPaymentService, AuditPaymentDecorator>();

        // 3. Configure Chains pattern
        services.AddChain<OrderProcessingRequest, OrderProcessingResponse>(
            typeof(OrderValidationChainHandler),
            typeof(InventoryChainHandler),
            typeof(PaymentChainHandler),
            typeof(FulfillmentChainHandler));
    }
}

// Main application service that demonstrates all patterns working together
public class ECommerceApplication
{
    private readonly IRequestMediator _mediator;
    private readonly IChainInvoker<OrderProcessingRequest, OrderProcessingResponse> _orderChain;
    private readonly ILogger<ECommerceApplication> _logger;

    public ECommerceApplication(
        IRequestMediator mediator,
        IChainInvoker<OrderProcessingRequest, OrderProcessingResponse> orderChain,
        ILogger<ECommerceApplication> logger)
    {
        _mediator = mediator;
        _orderChain = orderChain;
        _logger = logger;
    }

    public async Task RunDemoAsync()
    {
        _logger.LogInformation("Starting E-Commerce application demo");
        System.Console.WriteLine("Press key to continue...");
        System.Console.ReadKey();

        // Step 1: Create customer using Mediator pattern
        System.Console.WriteLine("1. Creating customer using Mediator pattern...");
        await _mediator.SendAsync(new CreateCustomerCommand("John Doe", "john@example.com"));
        
        var customer = await _mediator.SendAsync(new GetCustomerQuery(1));
        System.Console.WriteLine($"   Customer created: {customer.Name} ({customer.Email})");
        System.Console.WriteLine();
        System.Console.WriteLine("Press key to continue...");
        System.Console.ReadKey();

        // Step 2: Process order using both Mediator and Chains
        System.Console.WriteLine("2. Processing order using Mediator (which internally uses Chains)...");
        var orderResult = await _mediator.SendAsync(new ProcessOrderCommand(
            CustomerId: 1,
            ProductId: "LAPTOP-001",
            Quantity: 1,
            TotalAmount: 999.99m));
        
        System.Console.WriteLine($"   Order processed: {orderResult.OrderId} - Status: {orderResult.Status}");
        System.Console.WriteLine();
        System.Console.WriteLine("Press key to continue...");
        System.Console.ReadKey();

        // Step 3: Demonstrate chain execution directly
        System.Console.WriteLine("3. Running order fulfillment chain directly...");
        var chainRequest = new OrderProcessingRequest
        {
            OrderId = orderResult.OrderId,
            CustomerId = 1,
            ProductId = "LAPTOP-001",
            Quantity = 1,
            TotalAmount = 999.99m
        };

        var chainResponse = await _orderChain.HandleAsync(chainRequest);
        System.Console.WriteLine($"   Chain result: {chainResponse?.Status} - Tracking: {chainResponse?.TrackingNumber}");
        System.Console.WriteLine();
        System.Console.WriteLine("Press key to continue...");
        System.Console.ReadKey();

        // Step 4: Show service decoration in action
        System.Console.WriteLine("4. Demonstrating service decoration (automatic via DI)...");
        System.Console.WriteLine("   (Email and Payment services are automatically decorated with logging, retry, security, etc.)");
    }
}

// Business Models
public record Customer(int Id, string Name, string Email);
public record OrderResult(string OrderId, string Status);

// Mediator Commands and Queries
public record CreateCustomerCommand(string Name, string Email) : IRequest;
public record GetCustomerQuery(int CustomerId) : IRequest<Customer>;
public record ProcessOrderCommand(int CustomerId, string ProductId, int Quantity, decimal TotalAmount) : IRequest<OrderResult>;

// Chain Request/Response
public class OrderProcessingRequest
{
    public string OrderId { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
}

public class OrderProcessingResponse
{
    public string Status { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
}

// Business Services
public interface ICustomerService
{
    Task<Customer> CreateCustomerAsync(string name, string email);
    Task<Customer> GetCustomerAsync(int customerId);
}

public class CustomerService : ICustomerService
{
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ILogger<CustomerService> logger)
    {
        _logger = logger;
    }

    public async Task<Customer> CreateCustomerAsync(string name, string email)
    {
        _logger.LogInformation("Creating customer: {Name}", name);
        await Task.Delay(50); // Simulate database operation
        return new Customer(1, name, email);
    }

    public async Task<Customer> GetCustomerAsync(int customerId)
    {
        _logger.LogInformation("Getting customer: {CustomerId}", customerId);
        await Task.Delay(30);
        return new Customer(customerId, "John Doe", "john@example.com");
    }
}

public interface IProductService
{
    Task<bool> IsAvailableAsync(string productId, int quantity);
}

public class ProductService : IProductService
{
    public async Task<bool> IsAvailableAsync(string productId, int quantity)
    {
        await Task.Delay(40);
        return true; // Simulate availability check
    }
}

public interface IOrderService
{
    Task<string> CreateOrderAsync(int customerId, string productId, int quantity, decimal amount);
}

public class OrderService : IOrderService
{
    public async Task<string> CreateOrderAsync(int customerId, string productId, int quantity, decimal amount)
    {
        await Task.Delay(60);
        return $"ORD-{Random.Shared.Next(1000, 9999)}";
    }
}

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string body);
}

public class EmailService : IEmailService
{
    public async Task SendEmailAsync(string email, string subject, string body)
    {
        await Task.Delay(30);
        System.Console.WriteLine($"   [EmailService] Email sent to {email}: {subject}");
    }
}

public interface IPaymentService
{
    Task<bool> ProcessPaymentAsync(decimal amount, int customerId);
}

public class PaymentService : IPaymentService
{
    public async Task<bool> ProcessPaymentAsync(decimal amount, int customerId)
    {
        await Task.Delay(80);
        System.Console.WriteLine($"   [PaymentService] Payment processed: ${amount:F2}");
        return true;
    }
}

// Mediator Handlers
public class CreateCustomerHandler : IHandler<CreateCustomerCommand>
{
    private readonly ICustomerService _customerService;

    public CreateCustomerHandler(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task HandleAsync(CreateCustomerCommand request, CancellationToken cancellationToken = default)
    {
        await _customerService.CreateCustomerAsync(request.Name, request.Email);
    }
}

public class GetCustomerHandler : IHandler<GetCustomerQuery, Customer>
{
    private readonly ICustomerService _customerService;

    public GetCustomerHandler(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<Customer> HandleAsync(GetCustomerQuery request, CancellationToken cancellationToken = default)
    {
        return await _customerService.GetCustomerAsync(request.CustomerId);
    }
}

public class ProcessOrderHandler : IHandler<ProcessOrderCommand, OrderResult>
{
    private readonly IOrderService _orderService;
    private readonly IChainInvoker<OrderProcessingRequest, OrderProcessingResponse> _orderChain;

    public ProcessOrderHandler(
        IOrderService orderService,
        IChainInvoker<OrderProcessingRequest, OrderProcessingResponse> orderChain)
    {
        _orderService = orderService;
        _orderChain = orderChain;
    }

    public async Task<OrderResult> HandleAsync(ProcessOrderCommand request, CancellationToken cancellationToken = default)
    {
        // Create the order
        var orderId = await _orderService.CreateOrderAsync(
            request.CustomerId, 
            request.ProductId, 
            request.Quantity, 
            request.TotalAmount);

        // Process through chain for additional steps
        var chainRequest = new OrderProcessingRequest
        {
            OrderId = orderId,
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            TotalAmount = request.TotalAmount
        };

        var chainResponse = await _orderChain.HandleAsync(chainRequest, cancellationToken);
        
        return new OrderResult(orderId, chainResponse?.Status ?? "Created");
    }
}

// Chain Handlers
public class OrderValidationChainHandler : IChainHandler<OrderProcessingRequest, OrderProcessingResponse>
{
    public Task<bool> CanHandleAsync(OrderProcessingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<OrderProcessingResponse> HandleAsync(
        OrderProcessingRequest request, 
        Func<CancellationToken, Task<OrderProcessingResponse?>> next, 
        CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine("   [OrderValidationChain] Validating order...");
        await Task.Delay(20, cancellationToken);
        
        var response = await next(cancellationToken);
        return response ?? new OrderProcessingResponse();
    }
}

public class InventoryChainHandler : IChainHandler<OrderProcessingRequest, OrderProcessingResponse>
{
    private readonly IProductService _productService;

    public InventoryChainHandler(IProductService productService)
    {
        _productService = productService;
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
        System.Console.WriteLine("   [InventoryChain] Checking inventory...");
        await _productService.IsAvailableAsync(request.ProductId, request.Quantity);
        
        var response = await next(cancellationToken);
        return response ?? new OrderProcessingResponse();
    }
}

public class PaymentChainHandler : IChainHandler<OrderProcessingRequest, OrderProcessingResponse>
{
    private readonly IPaymentService _paymentService;

    public PaymentChainHandler(IPaymentService paymentService)
    {
        _paymentService = paymentService;
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
        System.Console.WriteLine("   [PaymentChain] Processing payment...");
        await _paymentService.ProcessPaymentAsync(request.TotalAmount, request.CustomerId);
        
        var response = await next(cancellationToken);
        return response ?? new OrderProcessingResponse();
    }
}

public class FulfillmentChainHandler : IChainHandler<OrderProcessingRequest, OrderProcessingResponse>
{
    private readonly IEmailService _emailService;

    public FulfillmentChainHandler(IEmailService emailService)
    {
        _emailService = emailService;
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
        System.Console.WriteLine("   [FulfillmentChain] Preparing fulfillment...");
        
        // Send confirmation email (using decorated email service)
        await _emailService.SendEmailAsync(
            "customer@example.com", 
            "Order Confirmation", 
            $"Your order {request.OrderId} is being processed");
        
        // This is the final handler in the chain
        return new OrderProcessingResponse
        {
            Status = "Fulfilled",
            TrackingNumber = $"TRK-{Random.Shared.Next(100000, 999999)}"
        };
    }
}

// Pipeline Behaviors
public class LoggingPreProcessor : IRequestPreProcessor<CreateCustomerCommand>,
                                  IRequestPreProcessor<GetCustomerQuery>,
                                  IRequestPreProcessor<ProcessOrderCommand>
{
    private readonly ILogger<LoggingPreProcessor> _logger;

    public LoggingPreProcessor(ILogger<LoggingPreProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(CreateCustomerCommand message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Pre-processing CreateCustomerCommand");
        return Task.CompletedTask;
    }

    public Task ProcessAsync(GetCustomerQuery message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Pre-processing GetCustomerQuery");
        return Task.CompletedTask;
    }

    public Task ProcessAsync(ProcessOrderCommand message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Pre-processing ProcessOrderCommand");
        return Task.CompletedTask;
    }
}

public class MetricsPostProcessor : IRequestPostProcessor<GetCustomerQuery, Customer>,
                                   IRequestPostProcessor<ProcessOrderCommand, OrderResult>
{
    private readonly ILogger<MetricsPostProcessor> _logger;

    public MetricsPostProcessor(ILogger<MetricsPostProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(GetCustomerQuery message, Customer response, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Post-processing GetCustomerQuery - Customer: {CustomerName}", response.Name);
        return Task.CompletedTask;
    }

    public Task ProcessAsync(ProcessOrderCommand message, OrderResult response, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Post-processing ProcessOrderCommand - Order: {OrderId}", response.OrderId);
        return Task.CompletedTask;
    }
}

// Service Decorators
public class LoggingEmailDecorator : IEmailService
{
    private readonly IEmailService _inner;
    private readonly ILogger<LoggingEmailDecorator> _logger;

    public LoggingEmailDecorator(IEmailService inner, ILogger<LoggingEmailDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        _logger.LogInformation("Sending email to {Email} with subject: {Subject}", email, subject);
        await _inner.SendEmailAsync(email, subject, body);
        _logger.LogInformation("Email sent successfully");
    }
}

public class RetryEmailDecorator : IEmailService
{
    private readonly IEmailService _inner;
    private readonly int _maxRetries = 3;

    public RetryEmailDecorator(IEmailService inner)
    {
        _inner = inner;
    }

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await _inner.SendEmailAsync(email, subject, body);
                return;
            }
            catch when (attempt < _maxRetries)
            {
                System.Console.WriteLine($"   [RetryEmailDecorator] Attempt {attempt} failed, retrying...");
                await Task.Delay(100 * attempt);
            }
        }
    }
}

public class SecurityPaymentDecorator : IPaymentService
{
    private readonly IPaymentService _inner;

    public SecurityPaymentDecorator(IPaymentService inner)
    {
        _inner = inner;
    }

    public async Task<bool> ProcessPaymentAsync(decimal amount, int customerId)
    {
        System.Console.WriteLine("   [SecurityPaymentDecorator] Performing security checks...");
        
        // Simulate security validation
        if (amount > 10000)
        {
            throw new UnauthorizedAccessException("Payment amount exceeds security threshold");
        }
        
        return await _inner.ProcessPaymentAsync(amount, customerId);
    }
}

public class AuditPaymentDecorator : IPaymentService
{
    private readonly IPaymentService _inner;
    private readonly ILogger<AuditPaymentDecorator> _logger;

    public AuditPaymentDecorator(IPaymentService inner, ILogger<AuditPaymentDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<bool> ProcessPaymentAsync(decimal amount, int customerId)
    {
        _logger.LogInformation("Audit: Payment processing started - Amount: {Amount}, Customer: {CustomerId}", amount, customerId);
        
        try
        {
            var result = await _inner.ProcessPaymentAsync(amount, customerId);
            _logger.LogInformation("Audit: Payment processing completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit: Payment processing failed");
            throw;
        }
    }
}
