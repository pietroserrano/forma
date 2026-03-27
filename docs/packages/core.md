# Forma.Core

**Forma.Core** is the foundation package that provides all core abstractions and interfaces for the Forma library. It has **zero external dependencies** and defines the contracts that all other packages implement.

[![NuGet](https://img.shields.io/nuget/v/Forma.Core.svg?label=Forma.Core)](https://www.nuget.org/packages/Forma.Core/)

## Installation

```bash
dotnet add package Forma.Core
```

## Overview

`Forma.Core` provides the fundamental building blocks used across all Forma packages. It defines the contracts (interfaces) that your application code depends on, keeping your domain logic independent of any concrete implementation.

## Key Abstractions

### `IRequest`

Marker interface for a request that produces **no return value** (command).

```csharp
using Forma.Core.Abstractions;

// A command that does not return a value
public record CreateUserCommand(string Name, string Email) : IRequest;
```

### `IRequest<TResponse>`

Marker interface for a request that produces a **typed return value** (query or command-with-response).

```csharp
// A query that returns a UserDto
public record GetUserQuery(int UserId) : IRequest<UserDto>;
public record UserDto(int Id, string Name, string Email);

// A command that returns the created entity's ID
public record CreateOrderCommand(string ProductName, int Quantity) : IRequest<int>;
```

### `IHandler<TRequest>`

Handler for a request with no return value.

```csharp
using Forma.Abstractions;

public class CreateUserCommandHandler : IHandler<CreateUserCommand>
{
    public Task HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        // Implement command logic here
        Console.WriteLine($"Creating user: {request.Name}");
        return Task.CompletedTask;
    }
}
```

### `IHandler<TRequest, TResponse>`

Handler for a request that returns a value.

```csharp
public class GetUserQueryHandler : IHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken cancellationToken = default)
    {
        // Fetch and return the user
        var user = new UserDto(request.UserId, "John Doe", "john@example.com");
        return Task.FromResult(user);
    }
}
```

### `IPipelineBehavior<TRequest, TResponse>`

Wraps handler execution to inject cross-cutting concerns (logging, validation, caching, etc.).

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next(cancellationToken);
        _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}
```

### `IRequestPreProcessor<TRequest>`

Runs **before** the handler for a specific request type.

```csharp
public class ValidationPreProcessor : IRequestPreProcessor<CreateUserCommand>
{
    public Task ProcessAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");
        return Task.CompletedTask;
    }
}
```

### `IRequestPostProcessor<TRequest, TResponse>`

Runs **after** the handler for a specific request type.

```csharp
public class AuditPostProcessor : IRequestPostProcessor<CreateUserCommand, Unit>
{
    public Task ProcessAsync(
        CreateUserCommand request,
        Unit response,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Audit: user '{request.Name}' was created.");
        return Task.CompletedTask;
    }
}
```

### `IRequestMediator`

The mediator contract used to dispatch requests.

```csharp
// Injected into your services/controllers
public class UserController
{
    private readonly IRequestMediator _mediator;

    public UserController(IRequestMediator mediator)
        => _mediator = mediator;

    public async Task<UserDto> GetUser(int id)
        => await _mediator.SendAsync(new GetUserQuery(id));
}
```

## Design Philosophy

- **Interface-first** — your application code depends only on abstractions from `Forma.Core`, never on implementation details from `Forma.Mediator`, `Forma.Decorator`, etc.
- **Zero dependencies** — `Forma.Core` references no external NuGet packages, keeping your domain model lightweight.
- **Composable** — combine any number of Forma packages without conflict; they all share the same core contracts.

## Related Packages

| Package | What it adds |
|---|---|
| [Forma.Mediator](/packages/mediator) | `IRequestMediator` implementation + DI registration |
| [Forma.Decorator](/packages/decorator) | `Decorate<TService, TDecorator>()` extension for DI |
| [Forma.Chains](/packages/chains) | `IChainInvoker<T>` + chain handler pipeline |
| [Forma.PubSub.InMemory](/packages/pubsub) | In-memory publish-subscribe messaging |
