# Forma.Decorator

**Forma.Decorator** provides a simple, DI-friendly way to apply the Decorator design pattern to .NET services. Wrap any registered service with one or more decorators to add cross-cutting concerns — logging, caching, validation, retry — **without modifying the original service**.

[![NuGet](https://img.shields.io/nuget/v/Forma.Decorator.svg?label=Forma.Decorator)](https://www.nuget.org/packages/Forma.Decorator/)

## Installation

```bash
dotnet add package Forma.Decorator
```

> `Forma.Decorator` depends on `Forma.Core` and `Microsoft.Extensions.DependencyInjection`.

## Core Concept

The `Decorate<TService, TDecorator>()` extension method re-registers an existing service so that it is automatically wrapped by the decorator class when resolved from the container.

```
DI Container resolves IOrderService as:

CachingOrderDecorator
  └─ ValidationOrderDecorator
       └─ LoggingOrderDecorator
            └─ OrderService  (inner implementation)
```

## Basic Usage

### 1. Define the service interface and base implementation

```csharp
public interface IOrderService
{
    Task<int> CreateOrderAsync(string productName, int quantity, string customerEmail);
}

public class OrderService : IOrderService
{
    public Task<int> CreateOrderAsync(string productName, int quantity, string customerEmail)
    {
        var orderId = Random.Shared.Next(1000, 9999);
        Console.WriteLine($"[OrderService] Order created: {orderId}");
        return Task.FromResult(orderId);
    }
}
```

### 2. Create a decorator

A decorator must accept the decorated service via its constructor:

```csharp
public class LoggingOrderDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ILogger<LoggingOrderDecorator> _logger;

    public LoggingOrderDecorator(IOrderService inner, ILogger<LoggingOrderDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<int> CreateOrderAsync(string productName, int quantity, string customerEmail)
    {
        _logger.LogInformation("Creating order for {Product} x{Qty}", productName, quantity);
        var result = await _inner.CreateOrderAsync(productName, quantity, customerEmail);
        _logger.LogInformation("Order {Id} created", result);
        return result;
    }
}
```

### 3. Register with Decorate

```csharp
using Forma.Decorator.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register the base service first
services.AddTransient<IOrderService, OrderService>();

// Wrap with decorators (innermost first, outermost last)
services.Decorate<IOrderService, LoggingOrderDecorator>();
services.Decorate<IOrderService, ValidationOrderDecorator>();
services.Decorate<IOrderService, CachingOrderDecorator>();

var provider = services.BuildServiceProvider();

// Resolves the full decorator chain automatically
var orderService = provider.GetRequiredService<IOrderService>();
```

## Decorator Examples

### Validation Decorator

```csharp
public class ValidationOrderDecorator : IOrderService
{
    private readonly IOrderService _inner;

    public ValidationOrderDecorator(IOrderService inner) => _inner = inner;

    public Task<int> CreateOrderAsync(string productName, int quantity, string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (!customerEmail.Contains('@'))
            throw new ArgumentException("Invalid email address.", nameof(customerEmail));

        return _inner.CreateOrderAsync(productName, quantity, customerEmail);
    }
}
```

### Retry Decorator

```csharp
public class RetryNotificationDecorator : INotificationService
{
    private readonly INotificationService _inner;
    private readonly ILogger<RetryNotificationDecorator> _logger;
    private const int MaxRetries = 3;

    public RetryNotificationDecorator(
        INotificationService inner,
        ILogger<RetryNotificationDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string message, string recipient)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await _inner.SendNotificationAsync(message, recipient);
                return;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed. Retrying...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }
    }
}
```

### Caching Decorator

```csharp
public class CachingOrderDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly IMemoryCache _cache;

    public CachingOrderDecorator(IOrderService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<int> CreateOrderAsync(string productName, int quantity, string customerEmail)
    {
        string cacheKey = $"order:{productName}:{quantity}:{customerEmail}";

        if (_cache.TryGetValue(cacheKey, out int cachedOrderId))
        {
            Console.WriteLine($"[Cache HIT] Returning cached order {cachedOrderId}");
            return cachedOrderId;
        }

        var orderId = await _inner.CreateOrderAsync(productName, quantity, customerEmail);
        _cache.Set(cacheKey, orderId, TimeSpan.FromMinutes(5));
        return orderId;
    }
}
```

### Audit Decorator

```csharp
public class AuditUserDecorator : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger<AuditUserDecorator> _logger;

    public AuditUserDecorator(IUserService inner, ILogger<AuditUserDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<User> GetUserAsync(int userId)
    {
        var user = await _inner.GetUserAsync(userId);
        _logger.LogInformation("[AUDIT] User {UserId} accessed at {Time}", userId, DateTime.UtcNow);
        return user;
    }

    public async Task UpdateUserAsync(int userId, string name)
    {
        _logger.LogInformation("[AUDIT] User {UserId} updated — new name: {Name}", userId, name);
        await _inner.UpdateUserAsync(userId, name);
    }
}
```

## Full Registration Example

```csharp
using Forma.Decorator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());
services.AddMemoryCache();

// Register base services
services.AddTransient<IOrderService, OrderService>();
services.AddTransient<INotificationService, EmailNotificationService>();
services.AddTransient<IUserService, UserService>();

// Apply decorators
services.Decorate<IOrderService, LoggingOrderDecorator>();
services.Decorate<IOrderService, ValidationOrderDecorator>();
services.Decorate<IOrderService, CachingOrderDecorator>();

services.Decorate<INotificationService, RetryNotificationDecorator>();
services.Decorate<INotificationService, LoggingNotificationDecorator>();

services.Decorate<IUserService, AuditUserDecorator>();

var provider = services.BuildServiceProvider();

// Use services normally — decorators are transparent
var orders = provider.GetRequiredService<IOrderService>();
await orders.CreateOrderAsync("Laptop", 1, "user@example.com");
```

## Decorator Order

The **last** decorator registered is the **outermost** one (first to execute):

```
Registration order:          Execution order (call):
1. Decorate<T, Logging>      CachingDecorator     (outermost, 3rd registered)
2. Decorate<T, Validation>   ValidationDecorator
3. Decorate<T, Caching>      LoggingDecorator
                             BaseService          (innermost, 1st registered)
```

::: tip
Register decorators from **least important** (e.g. logging) to **most important** (e.g. caching/security). The last registration wraps everything before it.
:::

## Related

- [Forma.Core](/packages/core) — Core abstractions
- [Console App Guide](/guides/console-app) — Full integration example
- [Web API Guide](/guides/web-api) — ASP.NET Core integration with decorators
