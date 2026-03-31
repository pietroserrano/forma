# Forma.Chains

**Forma.Chains** implements the Chain of Responsibility behavioral pattern for .NET. It routes a request through a configurable sequence of handlers where each handler can process the request, pass it along, or stop the chain early.

[![NuGet](https://img.shields.io/nuget/v/Forma.Chains.svg?label=Forma.Chains)](https://www.nuget.org/packages/Forma.Chains/)

## Installation

```bash
dotnet add package Forma.Chains
```

> `Forma.Chains` depends on `Forma.Core` and `Microsoft.Extensions.DependencyInjection`.

## Core Concept

A chain is an ordered list of handlers that process a request sequentially:

```
Request ──► Handler 1 ──► Handler 2 ──► Handler 3 ──► Handler 4 ──► [optional Response]
                           ▲ may stop here (early termination)
```

Forma supports two chain variants:

| Variant | Interface | Use case |
|---|---|---|
| **Void chain** | `IChainInvoker<TRequest>` | Sequential processing, no response needed |
| **Response chain** | `IChainInvoker<TRequest, TResponse>` | Sequential processing that produces a result |

## Void Chain (no response)

### 1. Define the request model

```csharp
public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public List<string> Results { get; set; } = new();
}
```

### 2. Implement chain handlers

Each handler receives the request and a `next` delegate. Call `next()` to continue the chain, or skip it to stop early.

```csharp
using Forma.Chains.Abstractions;

public class ValidationHandler : IChainHandler<PaymentRequest>
{
    public async Task HandleAsync(
        PaymentRequest request,
        Func<Task> next,
        CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be positive.");
        if (string.IsNullOrWhiteSpace(request.CardNumber))
            throw new ArgumentException("Card number is required.");

        request.Results.Add("Validation passed");
        await next(); // continue to the next handler
    }
}

public class FraudDetectionHandler : IChainHandler<PaymentRequest>
{
    public async Task HandleAsync(
        PaymentRequest request,
        Func<Task> next,
        CancellationToken ct = default)
    {
        if (request.Amount > 5000)
        {
            // Stop the chain — do NOT call next()
            request.Results.Add("Fraud detected — transaction blocked");
            return;
        }

        request.Results.Add("Fraud check passed");
        await next();
    }
}

public class PaymentProcessingHandler : IChainHandler<PaymentRequest>
{
    public async Task HandleAsync(
        PaymentRequest request,
        Func<Task> next,
        CancellationToken ct = default)
    {
        await Task.Delay(10, ct); // simulate async work
        request.Results.Add($"Payment of {request.Amount:C} processed");
        await next();
    }
}

public class NotificationHandler : IChainHandler<PaymentRequest>
{
    public async Task HandleAsync(
        PaymentRequest request,
        Func<Task> next,
        CancellationToken ct = default)
    {
        await Task.Delay(5, ct);
        request.Results.Add($"Notification sent to {request.CustomerEmail}");
        await next();
    }
}
```

### 3. Register and invoke the chain

```csharp
using Forma.Chains.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddChain<PaymentRequest>(
    typeof(ValidationHandler),
    typeof(FraudDetectionHandler),
    typeof(PaymentProcessingHandler),
    typeof(NotificationHandler));

var provider = services.BuildServiceProvider();

var chain = provider.GetRequiredService<IChainInvoker<PaymentRequest>>();

var request = new PaymentRequest
{
    Amount = 100.50m,
    CardNumber = "4532-1234-5678-9012",
    CustomerEmail = "john@example.com",
};

await chain.HandleAsync(request);

foreach (var step in request.Results)
    Console.WriteLine($"  ✓ {step}");
// Output:
//   ✓ Validation passed
//   ✓ Fraud check passed
//   ✓ Payment of $100.50 processed
//   ✓ Notification sent to john@example.com
```

## Response Chain (with response)

Use the response variant when the chain needs to produce a structured result.

### 1. Define request and response models

```csharp
public class OrderRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string CustomerId { get; set; } = string.Empty;
}

public class OrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

### 2. Implement response chain handlers

```csharp
public class OrderValidationHandler : IChainHandler<OrderRequest, OrderResponse>
{
    public async Task<OrderResponse?> HandleAsync(
        OrderRequest request,
        Func<Task<OrderResponse?>> next,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductId))
            return new OrderResponse { Status = "Validation failed — missing product ID" };

        return await next(); // pass to the next handler
    }
}

public class PricingHandler : IChainHandler<OrderRequest, OrderResponse>
{
    public async Task<OrderResponse?> HandleAsync(
        OrderRequest request,
        Func<Task<OrderResponse?>> next,
        CancellationToken ct = default)
    {
        // Compute pricing before passing along
        var response = await next() ?? new OrderResponse();
        response.TotalAmount = request.Quantity * 49.99m;
        return response;
    }
}

public class OrderCreationHandler : IChainHandler<OrderRequest, OrderResponse>
{
    public Task<OrderResponse?> HandleAsync(
        OrderRequest request,
        Func<Task<OrderResponse?>> next,
        CancellationToken ct = default)
    {
        var response = new OrderResponse
        {
            OrderId = $"ORD-{Random.Shared.Next(10000, 99999)}",
            Status = "Created",
        };
        return Task.FromResult<OrderResponse?>(response);
    }
}
```

### 3. Register and invoke

```csharp
services.AddChain<OrderRequest, OrderResponse>(
    typeof(OrderValidationHandler),
    typeof(InventoryCheckHandler),
    typeof(PricingHandler),
    typeof(OrderCreationHandler));

var chain = provider.GetRequiredService<IChainInvoker<OrderRequest, OrderResponse>>();

var request = new OrderRequest
{
    ProductId = "PROD-001",
    Quantity = 2,
    CustomerId = "CUST-123",
};

OrderResponse? response = await chain.HandleAsync(request);
Console.WriteLine($"Order: {response?.OrderId}, Total: {response?.TotalAmount:C}");
```

## Early Termination

A handler can **stop** the chain by returning without calling `next()`:

```csharp
// Void chain — stop by not awaiting next()
if (fraudDetected)
{
    request.Results.Add("Blocked by fraud check");
    return; // chain stops here
}

// Response chain — stop by returning a value directly
if (outOfStock)
{
    return new OrderResponse { Status = "Out of stock" }; // chain stops here
}
```

## Full Registration Example

```csharp
using Forma.Chains.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

// Void chain
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

// Resolve and invoke
var paymentChain = provider.GetRequiredService<IChainInvoker<PaymentRequest>>();
var orderChain   = provider.GetRequiredService<IChainInvoker<OrderRequest, OrderResponse>>();
```

## Related

- [Forma.Core](/packages/core) — Core abstractions
- [Console App Guide](/guides/console-app) — Full console integration example with chains
- [Web API Guide](/guides/web-api) — ASP.NET Core example with chains for order/payment flows
