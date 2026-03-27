# Console Application Guide

This guide shows how to integrate all Forma patterns into a .NET **console application** — from simple single-pattern usage to a complete multi-pattern integration.

## Prerequisites

```bash
dotnet new console -n MyApp
cd MyApp
dotnet add package Forma.Mediator
dotnet add package Forma.Decorator
dotnet add package Forma.Chains
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Logging.Console
```

---

## Mediator Pattern in a Console App

```csharp
using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

services.AddRequestMediator(config =>
{
    config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
    config.AddRequestPreProcessor<LoggingPreProcessor>();
    config.AddRequestPostProcessor<LoggingPostProcessor>();
});

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IRequestMediator>();

// Send a command (no response)
await mediator.SendAsync(new CreateUserCommand("John Doe", "john@example.com"));

// Send a query (with response)
var user = await mediator.SendAsync(new GetUserQuery(1));
Console.WriteLine($"Retrieved user: {user.Name} ({user.Email})");

// Send a command that returns a value
var orderId = await mediator.SendAsync(new CreateOrderCommand("Laptop", 2));
Console.WriteLine($"Created order with ID: {orderId}");

// ── Definitions ──────────────────────────────────────────────────────────────

public record CreateUserCommand(string Name, string Email) : IRequest;

public class CreateUserCommandHandler : IHandler<CreateUserCommand>
{
    private readonly ILogger<CreateUserCommandHandler> _logger;
    public CreateUserCommandHandler(ILogger<CreateUserCommandHandler> logger) => _logger = logger;

    public Task HandleAsync(CreateUserCommand request, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating user: {Name}", request.Name);
        Console.WriteLine($"✓ User '{request.Name}' created successfully!");
        return Task.CompletedTask;
    }
}

public record GetUserQuery(int UserId) : IRequest<UserDto>;
public record UserDto(int Id, string Name, string Email);

public class GetUserQueryHandler : IHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken ct = default)
        => Task.FromResult(new UserDto(request.UserId, "John Doe", "john@example.com"));
}

public record CreateOrderCommand(string ProductName, int Quantity) : IRequest<int>;

public class CreateOrderCommandHandler : IHandler<CreateOrderCommand, int>
{
    public Task<int> HandleAsync(CreateOrderCommand request, CancellationToken ct = default)
        => Task.FromResult(Random.Shared.Next(1000, 9999));
}

public class LoggingPreProcessor
    : IRequestPreProcessor<CreateUserCommand>,
      IRequestPreProcessor<GetUserQuery>,
      IRequestPreProcessor<CreateOrderCommand>
{
    public Task ProcessAsync(CreateUserCommand m, CancellationToken ct) { Console.WriteLine("[PRE] CreateUserCommand"); return Task.CompletedTask; }
    public Task ProcessAsync(GetUserQuery m, CancellationToken ct)     { Console.WriteLine("[PRE] GetUserQuery");     return Task.CompletedTask; }
    public Task ProcessAsync(CreateOrderCommand m, CancellationToken ct){ Console.WriteLine("[PRE] CreateOrderCommand"); return Task.CompletedTask; }
}

public class LoggingPostProcessor
    : IRequestPostProcessor<CreateUserCommand, Unit>,
      IRequestPostProcessor<GetUserQuery, UserDto>,
      IRequestPostProcessor<CreateOrderCommand, int>
{
    public Task ProcessAsync(CreateUserCommand r, Unit res, CancellationToken ct)      { Console.WriteLine("[POST] CreateUserCommand"); return Task.CompletedTask; }
    public Task ProcessAsync(GetUserQuery r, UserDto res, CancellationToken ct)        { Console.WriteLine("[POST] GetUserQuery");       return Task.CompletedTask; }
    public Task ProcessAsync(CreateOrderCommand r, int res, CancellationToken ct)      { Console.WriteLine("[POST] CreateOrderCommand"); return Task.CompletedTask; }
}
```

---

## Decorator Pattern in a Console App

```csharp
using Forma.Decorator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

// Register the base service
services.AddTransient<IOrderService, OrderService>();

// Apply decorators (innermost → outermost)
services.Decorate<IOrderService, LoggingOrderDecorator>();
services.Decorate<IOrderService, ValidationOrderDecorator>();
services.Decorate<IOrderService, CachingOrderDecorator>();

var provider = services.BuildServiceProvider();
var orderService = provider.GetRequiredService<IOrderService>();

// The call goes through: Caching → Validation → Logging → OrderService
var id = await orderService.CreateOrderAsync("Widget", 3, "alice@example.com");
Console.WriteLine($"Order ID: {id}");

// ── Definitions ──────────────────────────────────────────────────────────────

public interface IOrderService
{
    Task<int> CreateOrderAsync(string product, int qty, string email);
}

public class OrderService : IOrderService
{
    public Task<int> CreateOrderAsync(string product, int qty, string email)
    {
        Console.WriteLine($"[OrderService] Creating: {qty}x {product}");
        return Task.FromResult(Random.Shared.Next(1000, 9999));
    }
}

public class LoggingOrderDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ILogger<LoggingOrderDecorator> _logger;
    public LoggingOrderDecorator(IOrderService inner, ILogger<LoggingOrderDecorator> logger)
    { _inner = inner; _logger = logger; }

    public async Task<int> CreateOrderAsync(string product, int qty, string email)
    {
        _logger.LogInformation("[LOG] Creating order for {Product}", product);
        var result = await _inner.CreateOrderAsync(product, qty, email);
        _logger.LogInformation("[LOG] Order {Id} created", result);
        return result;
    }
}

public class ValidationOrderDecorator : IOrderService
{
    private readonly IOrderService _inner;
    public ValidationOrderDecorator(IOrderService inner) => _inner = inner;

    public Task<int> CreateOrderAsync(string product, int qty, string email)
    {
        if (string.IsNullOrWhiteSpace(product)) throw new ArgumentException("Product required.");
        if (qty <= 0) throw new ArgumentException("Quantity must be > 0.");
        if (!email.Contains('@')) throw new ArgumentException("Invalid email.");
        return _inner.CreateOrderAsync(product, qty, email);
    }
}

public class CachingOrderDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly Dictionary<string, int> _cache = new();
    public CachingOrderDecorator(IOrderService inner) => _inner = inner;

    public async Task<int> CreateOrderAsync(string product, int qty, string email)
    {
        var key = $"{product}:{qty}:{email}";
        if (_cache.TryGetValue(key, out var cached)) { Console.WriteLine("[CACHE HIT]"); return cached; }
        var result = await _inner.CreateOrderAsync(product, qty, email);
        _cache[key] = result;
        return result;
    }
}
```

---

## Chain of Responsibility in a Console App

```csharp
using Forma.Chains.Abstractions;
using Forma.Chains.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

// Void chain (no response)
services.AddChain<PaymentRequest>(
    typeof(ValidationHandler),
    typeof(FraudDetectionHandler),
    typeof(PaymentProcessingHandler),
    typeof(NotificationHandler));

// Response chain
services.AddChain<OrderRequest, OrderResponse>(
    typeof(OrderValidationHandler),
    typeof(InventoryCheckHandler),
    typeof(PricingHandler),
    typeof(OrderCreationHandler));

var provider = services.BuildServiceProvider();

// Invoke void chain
var paymentChain = provider.GetRequiredService<IChainInvoker<PaymentRequest>>();
var payment = new PaymentRequest { Amount = 100.50m, CardNumber = "4532-XXXX", CustomerEmail = "user@example.com" };
await paymentChain.HandleAsync(payment);
Console.WriteLine("Payment steps: " + string.Join(", ", payment.Results));

// Invoke response chain
var orderChain = provider.GetRequiredService<IChainInvoker<OrderRequest, OrderResponse>>();
var response = await orderChain.HandleAsync(new OrderRequest { ProductId = "PROD-1", Quantity = 2 });
Console.WriteLine($"Order: {response?.OrderId}, Total: {response?.TotalAmount:C}");

// ── Definitions ──────────────────────────────────────────────────────────────

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public List<string> Results { get; set; } = new();
}

public class ValidationHandler : IChainHandler<PaymentRequest>
{
    public async Task HandleAsync(PaymentRequest req, Func<Task> next, CancellationToken ct = default)
    {
        if (req.Amount <= 0) throw new ArgumentException("Invalid amount.");
        req.Results.Add("Validation passed");
        await next();
    }
}

public class FraudDetectionHandler : IChainHandler<PaymentRequest>
{
    public async Task HandleAsync(PaymentRequest req, Func<Task> next, CancellationToken ct = default)
    {
        if (req.Amount > 5000) { req.Results.Add("FRAUD BLOCKED"); return; }
        req.Results.Add("Fraud check passed");
        await next();
    }
}

public class PaymentProcessingHandler : IChainHandler<PaymentRequest>
{
    public async Task HandleAsync(PaymentRequest req, Func<Task> next, CancellationToken ct = default)
    {
        req.Results.Add($"Processed {req.Amount:C}");
        await next();
    }
}

public class NotificationHandler : IChainHandler<PaymentRequest>
{
    public async Task HandleAsync(PaymentRequest req, Func<Task> next, CancellationToken ct = default)
    {
        req.Results.Add($"Notified {req.CustomerEmail}");
        await next();
    }
}

public class OrderRequest { public string ProductId { get; set; } = ""; public int Quantity { get; set; } }
public class OrderResponse { public string OrderId { get; set; } = ""; public decimal TotalAmount { get; set; } public string Status { get; set; } = ""; }

public class OrderValidationHandler : IChainHandler<OrderRequest, OrderResponse>
{
    public async Task<OrderResponse?> HandleAsync(OrderRequest req, Func<Task<OrderResponse?>> next, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.ProductId))
            return new OrderResponse { Status = "Validation failed" };
        return await next();
    }
}

public class InventoryCheckHandler : IChainHandler<OrderRequest, OrderResponse>
{
    public async Task<OrderResponse?> HandleAsync(OrderRequest req, Func<Task<OrderResponse?>> next, CancellationToken ct = default)
    {
        // Simulate inventory check
        return await next();
    }
}

public class PricingHandler : IChainHandler<OrderRequest, OrderResponse>
{
    public async Task<OrderResponse?> HandleAsync(OrderRequest req, Func<Task<OrderResponse?>> next, CancellationToken ct = default)
    {
        var response = await next() ?? new OrderResponse();
        response.TotalAmount = req.Quantity * 49.99m;
        return response;
    }
}

public class OrderCreationHandler : IChainHandler<OrderRequest, OrderResponse>
{
    public Task<OrderResponse?> HandleAsync(OrderRequest req, Func<Task<OrderResponse?>> next, CancellationToken ct = default)
        => Task.FromResult<OrderResponse?>(new OrderResponse
        {
            OrderId = $"ORD-{Random.Shared.Next(10000, 99999)}",
            Status = "Created",
        });
}
```

---

## Complete Integration Example

The following example combines **Mediator + Decorator + Chains** in a single e-commerce console application using `Microsoft.Extensions.Hosting`:

```csharp
using Forma.Mediator.Extensions;
using Forma.Decorator.Extensions;
using Forma.Chains.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddLogging(b => { b.AddConsole(); b.SetMinimumLevel(LogLevel.Information); });

        // Application services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ECommerceApplication>();

        // 1. Mediator
        services.AddRequestMediator(config =>
        {
            config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
            config.AddRequestPreProcessor<LoggingPreProcessor>();
        });

        // 2. Decorators on payment & email services
        services.Decorate<IPaymentService, SecurityPaymentDecorator>();
        services.Decorate<IPaymentService, AuditPaymentDecorator>();
        services.Decorate<IEmailService, RetryEmailDecorator>();
        services.Decorate<IEmailService, LoggingEmailDecorator>();

        // 3. Order processing chain
        services.AddChain<OrderProcessingRequest, OrderProcessingResponse>(
            typeof(OrderValidationChainHandler),
            typeof(InventoryChainHandler),
            typeof(PaymentChainHandler),
            typeof(FulfillmentChainHandler));
    })
    .Build();

using var scope = host.Services.CreateScope();
var app = scope.ServiceProvider.GetRequiredService<ECommerceApplication>();
await app.RunAsync();
```

### Running the examples from the repository

The repository contains ready-to-run example projects:

```bash
# Mediator pattern
dotnet run --project examples/console/Forma.Examples.Console.Mediator

# Decorator pattern
dotnet run --project examples/console/Forma.Examples.Console.Decorator

# Chain of Responsibility
dotnet run --project examples/console/Forma.Examples.Console.Chains

# Complete integration (all patterns)
dotnet run --project examples/console/Forma.Examples.Console.DependencyInjection
```

---

## See Also

- [Forma.Mediator docs](/packages/mediator)
- [Forma.Decorator docs](/packages/decorator)
- [Forma.Chains docs](/packages/chains)
- [Web API Guide](/guides/web-api)
