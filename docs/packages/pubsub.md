# Forma.PubSub.InMemory

**Forma.PubSub.InMemory** provides an in-memory, event-driven Publish-Subscribe messaging system for .NET applications. It decouples event producers from event consumers and integrates seamlessly with `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Logging`.

[![NuGet](https://img.shields.io/nuget/v/Forma.PubSub.InMemory.svg?label=Forma.PubSub.InMemory)](https://www.nuget.org/packages/Forma.PubSub.InMemory/)

## Installation

```bash
dotnet add package Forma.PubSub.InMemory
```

> This package depends on `Forma.Core`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Hosting`, and `Microsoft.Extensions.Logging`.

## Core Concept

The Publish-Subscribe pattern separates the event **publisher** from the event **subscriber**:

```
Publisher                 Bus                  Subscribers
─────────────────────────────────────────────────────────
OrderService   ──publishes──► InMemoryBus ──► SendEmailSubscriber
                                         ──► UpdateInventorySubscriber
                                         ──► AuditLogSubscriber
```

Publishers fire-and-forget: they do not know about, and do not wait for, subscribers.

## Defining Events

Events are plain record/class types that carry the data to be communicated.

```csharp
// An event raised when an order is placed
public record OrderPlacedEvent(
    string OrderId,
    string CustomerId,
    decimal TotalAmount,
    DateTime PlacedAt);

// An event raised when a user registers
public record UserRegisteredEvent(
    int UserId,
    string Name,
    string Email,
    DateTime RegisteredAt);
```

## Implementing Subscribers

A subscriber implements `IEventHandler<TEvent>` and is registered with DI.

```csharp
using Forma.Core.Abstractions;

public class SendWelcomeEmailSubscriber : IEventHandler<UserRegisteredEvent>
{
    private readonly ILogger<SendWelcomeEmailSubscriber> _logger;

    public SendWelcomeEmailSubscriber(ILogger<SendWelcomeEmailSubscriber> logger)
        => _logger = logger;

    public Task HandleAsync(UserRegisteredEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Sending welcome email to {Email} for user {UserId}",
            @event.Email, @event.UserId);

        // Send the email here ...
        return Task.CompletedTask;
    }
}

public class OrderPlacedInventorySubscriber : IEventHandler<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedInventorySubscriber> _logger;

    public OrderPlacedInventorySubscriber(ILogger<OrderPlacedInventorySubscriber> logger)
        => _logger = logger;

    public Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Updating inventory for order {OrderId}",
            @event.OrderId);

        // Deduct stock here ...
        return Task.CompletedTask;
    }
}
```

## Registration

Register the in-memory publisher and your subscribers with `AddInMemoryPubSub` (or directly via DI):

```csharp
using Forma.PubSub.InMemory.Extensions; // (if extension method available)
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLogging(b => b.AddConsole());

        // Register the PubSub infrastructure
        services.AddInMemoryPubSub(config =>
        {
            config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
        });
    })
    .Build();
```

If no extension method is available, register manually:

```csharp
services.AddScoped<IEventHandler<UserRegisteredEvent>, SendWelcomeEmailSubscriber>();
services.AddScoped<IEventHandler<OrderPlacedEvent>, OrderPlacedInventorySubscriber>();
// ... register the publisher implementation
```

## Publishing Events

Inject `IEventPublisher` (or `IPublisher`) into your service and publish events:

```csharp
public class OrderService
{
    private readonly IEventPublisher _publisher;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IEventPublisher publisher, ILogger<OrderService> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PlaceOrderAsync(string customerId, decimal amount)
    {
        var orderId = $"ORD-{Random.Shared.Next(10000, 99999)}";

        // ... business logic ...

        // Publish the event — all subscribers are notified
        await _publisher.PublishAsync(new OrderPlacedEvent(
            OrderId: orderId,
            CustomerId: customerId,
            TotalAmount: amount,
            PlacedAt: DateTime.UtcNow));

        _logger.LogInformation("Order {OrderId} placed and event published.", orderId);
    }
}
```

## Multiple Subscribers for the Same Event

Register any number of subscribers for the same event type. All are invoked when the event is published.

```csharp
// All three will be called when OrderPlacedEvent is published
services.AddScoped<IEventHandler<OrderPlacedEvent>, OrderPlacedInventorySubscriber>();
services.AddScoped<IEventHandler<OrderPlacedEvent>, OrderPlacedEmailSubscriber>();
services.AddScoped<IEventHandler<OrderPlacedEvent>, OrderPlacedAuditSubscriber>();
```

## Background Service Integration

For long-running background processing, implement a hosted subscriber:

```csharp
public class OrderEventBackgroundService : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<OrderEventBackgroundService> _logger;

    public OrderEventBackgroundService(
        IServiceProvider provider,
        ILogger<OrderEventBackgroundService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order event background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Process queued events...
            await Task.Delay(1000, stoppingToken);
        }
    }
}

// Register as hosted service
services.AddHostedService<OrderEventBackgroundService>();
```

## Full Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLogging(b =>
        {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Information);
        });

        // Register subscribers
        services.AddScoped<IEventHandler<UserRegisteredEvent>, SendWelcomeEmailSubscriber>();
        services.AddScoped<IEventHandler<OrderPlacedEvent>, OrderPlacedInventorySubscriber>();
        services.AddScoped<IEventHandler<OrderPlacedEvent>, OrderPlacedEmailSubscriber>();

        // Register application services
        services.AddScoped<OrderService>();
        services.AddScoped<UserService>();
    })
    .Build();

using var scope = host.Services.CreateScope();

var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
await orderService.PlaceOrderAsync("CUST-001", 299.99m);

var userService = scope.ServiceProvider.GetRequiredService<UserService>();
await userService.RegisterAsync("Jane Doe", "jane@example.com");
```

## Related

- [Forma.Core](/packages/core) — Core abstractions (`IEventHandler<T>`)
- [Console App Guide](/guides/console-app) — Broader integration example
- [Web API Guide](/guides/web-api) — Event-driven notifications in ASP.NET Core
