# Forma.Mediator

**Forma.Mediator** implements the Mediator behavioral pattern for .NET applications. It provides a simple, in-process messaging infrastructure that decouples request senders from their handlers and supports a rich pipeline of cross-cutting behaviors.

[![NuGet](https://img.shields.io/nuget/v/Forma.Mediator.svg?label=Forma.Mediator)](https://www.nuget.org/packages/Forma.Mediator/)

## Installation

```bash
dotnet add package Forma.Mediator
```

> `Forma.Mediator` depends on `Forma.Core` (installed automatically) and `Microsoft.Extensions.DependencyInjection`.

## Registration

Register the mediator in your DI container with `AddRequestMediator`:

```csharp
using Forma.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddRequestMediator(config =>
{
    // Auto-scan and register all handlers from the given assemblies
    config.RegisterServicesFromAssemblies(typeof(Program).Assembly);

    // Optionally add pipeline behaviors, pre/post-processors
    config.AddRequestPreProcessor<ValidationPreProcessor>();
    config.AddRequestPostProcessor<AuditPostProcessor>();
});
```

## Commands (no response)

A **command** is a request that performs an action and returns nothing.

```csharp
using Forma.Core.Abstractions;
using Forma.Abstractions;

// 1. Define the command
public record CreateUserCommand(string Name, string Email) : IRequest;

// 2. Implement the handler
public class CreateUserCommandHandler : IHandler<CreateUserCommand>
{
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(ILogger<CreateUserCommandHandler> logger)
        => _logger = logger;

    public Task HandleAsync(CreateUserCommand request, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating user {Name} ({Email})", request.Name, request.Email);
        // ... persist to database, raise events, etc.
        return Task.CompletedTask;
    }
}

// 3. Send the command
await mediator.SendAsync(new CreateUserCommand("Alice", "alice@example.com"));
```

## Queries (with response)

A **query** is a request that returns data without producing side effects.

```csharp
// 1. Define the query and its response DTO
public record GetUserQuery(int UserId) : IRequest<UserDto>;
public record UserDto(int Id, string Name, string Email);

// 2. Implement the handler
public class GetUserQueryHandler : IHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken ct = default)
    {
        // Simulate a DB lookup
        var user = new UserDto(request.UserId, "John Doe", "john@example.com");
        return Task.FromResult(user);
    }
}

// 3. Send the query
UserDto user = await mediator.SendAsync(new GetUserQuery(42));
Console.WriteLine($"Found: {user.Name}");
```

## Commands with response

Commands can also return a value (e.g., the ID of the newly created entity):

```csharp
public record CreateOrderCommand(string ProductName, int Quantity) : IRequest<int>;

public class CreateOrderCommandHandler : IHandler<CreateOrderCommand, int>
{
    public Task<int> HandleAsync(CreateOrderCommand request, CancellationToken ct = default)
    {
        var orderId = Random.Shared.Next(1000, 9999);
        return Task.FromResult(orderId);
    }
}

int orderId = await mediator.SendAsync(new CreateOrderCommand("Laptop", 1));
```

## Pipeline Behaviors

Pipeline behaviors wrap handler execution and are ideal for cross-cutting concerns. They are executed in registration order (first registered = outermost).

```csharp
public class TimingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var response = await next(ct);
        sw.Stop();
        Console.WriteLine($"{typeof(TRequest).Name} took {sw.ElapsedMilliseconds} ms");
        return response;
    }
}
```

Register behaviors when configuring the mediator:

```csharp
services.AddRequestMediator(config =>
{
    config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
    // Behaviors execute in the order they are registered
    config.AddBehavior(typeof(TimingBehavior<,>));
    config.AddBehavior(typeof(ValidationBehavior<,>));
});
```

## Pre-processors

Pre-processors run **before** the handler for a specific request type. Useful for validation.

```csharp
public class CreateUserValidationPreProcessor
    : IRequestPreProcessor<CreateUserCommand>
{
    public Task ProcessAsync(CreateUserCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.", nameof(request.Name));
        if (!request.Email.Contains('@'))
            throw new ArgumentException("Invalid email address.", nameof(request.Email));
        return Task.CompletedTask;
    }
}
```

```csharp
services.AddRequestMediator(config =>
{
    config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
    config.AddRequestPreProcessor<CreateUserValidationPreProcessor>();
});
```

## Post-processors

Post-processors run **after** the handler. Useful for auditing, notifications, and cache invalidation.

```csharp
public class CreateUserAuditPostProcessor
    : IRequestPostProcessor<CreateUserCommand, Unit>
{
    public Task ProcessAsync(
        CreateUserCommand request,
        Unit response,
        CancellationToken ct)
    {
        Console.WriteLine($"[AUDIT] User '{request.Name}' created at {DateTime.UtcNow:O}");
        return Task.CompletedTask;
    }
}
```

## Auto-registration

`RegisterServicesFromAssemblies` scans the given assemblies and automatically registers:

- All `IHandler<TRequest>` implementations
- All `IHandler<TRequest, TResponse>` implementations
- All pre/post-processors referenced via `config.AddRequestPreProcessor<T>()`

```csharp
config.RegisterServicesFromAssemblies(
    typeof(Program).Assembly,
    typeof(SomeOtherAssemblyMarker).Assembly);
```

## Full Example

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

// Command
await mediator.SendAsync(new CreateUserCommand("John Doe", "john@example.com"));

// Query
var user = await mediator.SendAsync(new GetUserQuery(1));
Console.WriteLine($"Retrieved: {user.Name}");

// Command with response
int orderId = await mediator.SendAsync(new CreateOrderCommand("Widget", 3));
Console.WriteLine($"Order ID: {orderId}");
```

## Related

- [Forma.Core](/packages/core) — Abstractions used by this package
- [Console App Guide](/guides/console-app) — Complete console application example
- [Web API Guide](/guides/web-api) — Complete ASP.NET Core example
