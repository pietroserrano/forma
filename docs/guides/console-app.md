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

## Functional Programming in a Console App

### Basic Result and Option Usage

```csharp
using Forma.Core.FP;

// ── Result: Railway-Oriented Programming ────────────────────────────────────

// Simple success/failure pipeline
var result = Result<int>.Success(10)
    .Then(x => Result<int>.Success(x * 2))
    .Then(x => x > 15 ? Result<int>.Success(x) : Result<int>.Failure(Error.BusinessRule("TooSmall", "Too small")))
    .Then(x => Result<int>.Success(x + 5))
    .Match(
        onSuccess: value => $"Final value: {value}",
        onFailure: error => $"Error: {error.Message}");

Console.WriteLine(result);
// Output: Final value: 25

// ── Option: Safe null handling ───────────────────────────────────────────────

var config = new Dictionary<string, string> { ["Theme"] = "Dark" };

var theme = Option<string>.From(config.TryGetValue("Theme", out var t) ? t : null)
    .Then(t => Option<string>.Some(t.ToUpper()))
    .Match(
        some: value => $"Using theme: {value}",
        none: () => "Using default theme");

Console.WriteLine(theme);
// Output: Using theme: DARK
```

### Form Validation with Result

```csharp
using Forma.Core.FP;
using System.Text.RegularExpressions;

public record RegisterUserRequest(string Username, string Email, int Age);
public record User(string Username, string Email, int Age);

public class UserRegistrationService
{
    public Result<User> RegisterUser(RegisterUserRequest request)
    {
        return ValidateUsername(request.Username)
            .Then(_ => ValidateEmail(request.Email))
            .Then(_ => ValidateAge(request.Age))
            .Then(_ => Result<User>.Success(new User(request.Username, request.Email, request.Age)));
    }

    private Result<string> ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<string>.Failure(Error.Validation("username", "Username is required."));
        if (username.Length < 3)
            return Result<string>.Failure(Error.Validation("username", "Username must be at least 3 characters."));
        return Result<string>.Success(username);
    }

    private Result<string> ValidateEmail(string email)
    {
        if (!email.Contains('@'))
            return Result<string>.Failure(Error.Validation("email", "Invalid email address."));
        return Result<string>.Success(email);
    }

    private Result<int> ValidateAge(int age)
    {
        if (age < 18)
            return Result<int>.Failure(Error.Validation("age", "Must be at least 18 years old."));
        return Result<int>.Success(age);
    }
}

// Usage
var service = new UserRegistrationService();

var request1 = new RegisterUserRequest("john", "john@example.com", 25);
var result1 = service.RegisterUser(request1);
Console.WriteLine(result1.Match(
    onSuccess: user => $"✓ User '{user.Username}' registered successfully!",
    onFailure: error => $"✗ Registration failed: {error}"
));
// Output: ✓ User 'john' registered successfully!

var request2 = new RegisterUserRequest("jane", "invalid-email", 25);
var result2 = service.RegisterUser(request2);
Console.WriteLine(result2.Match(
    onSuccess: user => $"✓ User '{user.Username}' registered successfully!",
    onFailure: error => $"✗ Registration failed: {error}"
));
// Output: ✗ Registration failed: Invalid email address.
```

### File Operations with Result

```csharp
using Forma.Core.FP;

public class FileService
{
    public Result<string> ReadFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return Result<string>.Failure(Error.Generic($"File not found: {path}"));
            
            var content = File.ReadAllText(path);
            return Result<string>.Success(content);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(Error.Generic($"Error reading file: {ex.Message}"));
        }
    }

    public Result<int> WriteFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
            return Result<int>.Success(content.Length);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(Error.Generic($"Error writing file: {ex.Message}"));
        }
    }

    public Result<string> ProcessFile(string inputPath, string outputPath)
    {
        return ReadFile(inputPath)
            .Then(content => Result<string>.Success(content.ToUpper()))
            .Do(processedContent => Console.WriteLine($"Processed {processedContent.Length} characters"))
            .Then(processedContent => 
                WriteFile(outputPath, processedContent)
                    .Then(_ => processedContent));
    }
}

// Usage
var fileService = new FileService();

var result = fileService.ProcessFile("input.txt", "output.txt")
    .Match(
        onSuccess: content => $"✓ File processed successfully: {content.Length} bytes",
        onFailure: error => $"✗ Processing failed: {error}"
    );

Console.WriteLine(result);
```

### Configuration Management with Option

```csharp
using Forma.Core.FP;

public class AppConfiguration
{
    private readonly Dictionary<string, string> _settings = new();

    public Option<string> GetSetting(string key)
    {
        return _settings.TryGetValue(key, out var value)
            ? Option<string>.Some(value)
            : Option<string>.None();
    }

    public void SetSetting(string key, string value) => _settings[key] = value;

    public string GetRequiredSetting(string key, string defaultValue)
    {
        return GetSetting(key).Match(
            some: value => value,
            none: () => defaultValue
        );
    }

    public Result<int> GetIntSetting(string key)
    {
        return GetSetting(key)
            .Match(
                some: value => int.TryParse(value, out var intValue)
                    ? Result<int>.Success(intValue)
                    : Result<int>.Failure(Error.DataFormat(key, "integer", value)),
                none: () => Result<int>.Failure(Error.Generic($"Setting '{key}' not found"))
            );
    }
}

// Usage
var config = new AppConfiguration();
config.SetSetting("MaxRetries", "3");
config.SetSetting("InvalidNumber", "abc");

// Get optional setting
var theme = config.GetSetting("Theme")
    .Match(
        some: v => $"Theme: {v}",
        none: () => "Theme: default"
    );
Console.WriteLine(theme);
// Output: Theme: default

// Get required setting with default
var timeout = config.GetRequiredSetting("Timeout", "30");
Console.WriteLine($"Timeout: {timeout}");
// Output: Timeout: 30

// Parse integer setting
var maxRetries = config.GetIntSetting("MaxRetries")
    .Match(
        onSuccess: value => $"Max retries: {value}",
        onFailure: error => $"Error: {error}"
    );
Console.WriteLine(maxRetries);
// Output: Max retries: 3

var invalid = config.GetIntSetting("InvalidNumber")
    .Match(
        onSuccess: value => $"Value: {value}",
        onFailure: error => $"Error: {error}"
    );
Console.WriteLine(invalid);
// Output: Error: 'abc' is not a valid integer
```

### Running the FP Examples

The repository contains a ready-to-run FP example project:

```bash
dotnet run --project examples/console/Forma.Examples.Console.FP
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
- [Forma.Core FP docs](/packages/fp)
- [Web API Guide](/guides/web-api)
