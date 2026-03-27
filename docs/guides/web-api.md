# ASP.NET Core Web API Guide

This guide shows how to integrate Forma into an **ASP.NET Core Web API** application using Minimal APIs. You will see how Mediator, Decorator, and Chains work together in a realistic REST API for user and order management.

## Prerequisites

```bash
dotnet new webapi -n MyApi --use-minimal-apis
cd MyApi
dotnet add package Forma.Mediator
dotnet add package Forma.Decorator
dotnet add package Forma.Chains
```

---

## Project Setup (`Program.cs`)

```csharp
using Forma.Mediator.Extensions;
using Forma.Decorator.Extensions;
using Forma.Chains.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── 1. Mediator ──────────────────────────────────────────────────────────────
builder.Services.AddRequestMediator(config =>
{
    // Automatically register all handlers in this assembly
    config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
});

// ── 2. Application services + Decorators ─────────────────────────────────────
builder.Services.AddScoped<IUserService, UserService>();

// Decorator chain (registration order = innermost → outermost):
// Request flow: LoggingDecorator → ValidationDecorator → CachingDecorator → UserService
builder.Services.Decorate<IUserService, CachingUserServiceDecorator>();
builder.Services.Decorate<IUserService, ValidationUserServiceDecorator>();
builder.Services.Decorate<IUserService, LoggingUserServiceDecorator>();

// ── 3. Chains ────────────────────────────────────────────────────────────────
builder.Services.AddChain<OrderProcessingRequest, OrderProcessingResponse>(
    typeof(OrderValidationHandler),
    typeof(InventoryCheckHandler),
    typeof(OrderPricingHandler),
    typeof(OrderCreationHandler));

builder.Services.AddChain<PaymentProcessingRequest>(
    typeof(PaymentValidationHandler),
    typeof(PaymentFraudDetectionHandler),
    typeof(PaymentProcessingHandler),
    typeof(PaymentNotificationHandler));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map endpoint groups
app.MapUserEndpoints();
app.MapOrderEndpoints();
app.MapHealthEndpoints();

app.Run();
```

---

## CQRS with Minimal APIs

### Commands and Queries

```csharp
using Forma.Core.Abstractions;

// Commands (write operations)
public record CreateUserCommand(string Name, string Email) : IRequest<UserCreatedResponse>;
public record UpdateUserCommand(int Id, string Name, string Email) : IRequest<UserDto>;
public record DeleteUserCommand(int Id) : IRequest;

// Queries (read operations)
public record GetUserQuery(int UserId) : IRequest<UserDto>;
public record GetAllUsersQuery() : IRequest<List<UserDto>>;

// DTOs
public record UserDto(int Id, string Name, string Email);
public record UserCreatedResponse(int Id, string Message);
```

### Handlers

```csharp
using Forma.Abstractions;

public class CreateUserCommandHandler : IHandler<CreateUserCommand, UserCreatedResponse>
{
    private readonly IUserService _users;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(IUserService users, ILogger<CreateUserCommandHandler> logger)
    { _users = users; _logger = logger; }

    public async Task<UserCreatedResponse> HandleAsync(
        CreateUserCommand request, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating user {Name}", request.Name);
        var user = await _users.CreateUserAsync(request.Name, request.Email);
        return new UserCreatedResponse(user.Id, $"User '{user.Name}' created successfully");
    }
}

public class GetUserQueryHandler : IHandler<GetUserQuery, UserDto>
{
    private readonly IUserService _users;

    public GetUserQueryHandler(IUserService users) => _users = users;

    public async Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken ct = default)
    {
        var user = await _users.GetUserAsync(request.UserId)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");
        return user;
    }
}

public class GetAllUsersQueryHandler : IHandler<GetAllUsersQuery, List<UserDto>>
{
    private readonly IUserService _users;
    public GetAllUsersQueryHandler(IUserService users) => _users = users;

    public Task<List<UserDto>> HandleAsync(GetAllUsersQuery request, CancellationToken ct = default)
        => _users.GetAllUsersAsync();
}
```

### Minimal API endpoints

```csharp
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/", async (IRequestMediator mediator) =>
        {
            var users = await mediator.SendAsync(new GetAllUsersQuery());
            return Results.Ok(users);
        })
        .WithSummary("Get all users")
        .Produces<List<UserDto>>();

        group.MapGet("/{id:int}", async (int id, IRequestMediator mediator) =>
        {
            try
            {
                var user = await mediator.SendAsync(new GetUserQuery(id));
                return Results.Ok(user);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .WithSummary("Get user by ID")
        .Produces<UserDto>()
        .Produces(404);

        group.MapPost("/", async (CreateUserRequest body, IRequestMediator mediator) =>
        {
            var result = await mediator.SendAsync(new CreateUserCommand(body.Name, body.Email));
            return Results.Created($"/api/users/{result.Id}", result);
        })
        .WithSummary("Create a new user")
        .Accepts<CreateUserRequest>("application/json")
        .Produces<UserCreatedResponse>(201)
        .Produces(400);

        group.MapPut("/{id:int}", async (int id, CreateUserRequest body, IRequestMediator mediator) =>
        {
            try
            {
                var user = await mediator.SendAsync(new UpdateUserCommand(id, body.Name, body.Email));
                return Results.Ok(user);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .WithSummary("Update a user")
        .Produces<UserDto>()
        .Produces(404);

        group.MapDelete("/{id:int}", async (int id, IRequestMediator mediator) =>
        {
            try
            {
                await mediator.SendAsync(new DeleteUserCommand(id));
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .WithSummary("Delete a user")
        .Produces(204)
        .Produces(404);
    }
}

public record CreateUserRequest(string Name, string Email);
```

---

## Service Decorators in ASP.NET Core

```csharp
// Base service interface
public interface IUserService
{
    Task<UserDto> CreateUserAsync(string name, string email);
    Task<UserDto?> GetUserAsync(int id);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto> UpdateUserAsync(int id, string name, string email);
    Task DeleteUserAsync(int id);
}

// Logging decorator
public class LoggingUserServiceDecorator : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger<LoggingUserServiceDecorator> _logger;

    public LoggingUserServiceDecorator(IUserService inner, ILogger<LoggingUserServiceDecorator> logger)
    { _inner = inner; _logger = logger; }

    public async Task<UserDto> CreateUserAsync(string name, string email)
    {
        _logger.LogInformation("[LOG] CreateUser: {Name}", name);
        var result = await _inner.CreateUserAsync(name, email);
        _logger.LogInformation("[LOG] User {Id} created", result.Id);
        return result;
    }

    // delegate remaining methods to _inner...
    public Task<UserDto?> GetUserAsync(int id) => _inner.GetUserAsync(id);
    public Task<List<UserDto>> GetAllUsersAsync() => _inner.GetAllUsersAsync();
    public Task<UserDto> UpdateUserAsync(int id, string name, string email) => _inner.UpdateUserAsync(id, name, email);
    public Task DeleteUserAsync(int id) => _inner.DeleteUserAsync(id);
}

// Validation decorator
public class ValidationUserServiceDecorator : IUserService
{
    private readonly IUserService _inner;
    public ValidationUserServiceDecorator(IUserService inner) => _inner = inner;

    public Task<UserDto> CreateUserAsync(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");
        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email address.");
        return _inner.CreateUserAsync(name, email);
    }

    public Task<UserDto?> GetUserAsync(int id)
    {
        if (id <= 0) throw new ArgumentException("Invalid user ID.");
        return _inner.GetUserAsync(id);
    }

    public Task<List<UserDto>> GetAllUsersAsync() => _inner.GetAllUsersAsync();
    public Task<UserDto> UpdateUserAsync(int id, string name, string email) => _inner.UpdateUserAsync(id, name, email);
    public Task DeleteUserAsync(int id) => _inner.DeleteUserAsync(id);
}

// Caching decorator
public class CachingUserServiceDecorator : IUserService
{
    private readonly IUserService _inner;
    private readonly IMemoryCache _cache;

    public CachingUserServiceDecorator(IUserService inner, IMemoryCache cache)
    { _inner = inner; _cache = cache; }

    public Task<UserDto?> GetUserAsync(int id)
    {
        if (_cache.TryGetValue($"user:{id}", out UserDto? cached))
            return Task.FromResult(cached);
        return _inner.GetUserAsync(id);
    }

    public Task<UserDto> CreateUserAsync(string name, string email) => _inner.CreateUserAsync(name, email);
    public Task<List<UserDto>> GetAllUsersAsync() => _inner.GetAllUsersAsync();
    public Task<UserDto> UpdateUserAsync(int id, string name, string email) => _inner.UpdateUserAsync(id, name, email);
    public Task DeleteUserAsync(int id) => _inner.DeleteUserAsync(id);
}
```

---

## Order Processing Chain

```csharp
// Models
public class OrderProcessingRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
}

public class OrderProcessingResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
}

// Handlers
public class OrderValidationHandler : IChainHandler<OrderProcessingRequest, OrderProcessingResponse>
{
    public async Task<OrderProcessingResponse?> HandleAsync(
        OrderProcessingRequest request,
        Func<Task<OrderProcessingResponse?>> next,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductId))
            return new OrderProcessingResponse { Status = "Validation failed" };
        if (request.Quantity <= 0)
            return new OrderProcessingResponse { Status = "Invalid quantity" };
        return await next();
    }
}

public class OrderCreationHandler : IChainHandler<OrderProcessingRequest, OrderProcessingResponse>
{
    public Task<OrderProcessingResponse?> HandleAsync(
        OrderProcessingRequest request,
        Func<Task<OrderProcessingResponse?>> next,
        CancellationToken ct = default)
    {
        return Task.FromResult<OrderProcessingResponse?>(new OrderProcessingResponse
        {
            OrderId = $"ORD-{Random.Shared.Next(10000, 99999)}",
            Status = "Created",
            TrackingNumber = $"TRK-{Guid.NewGuid():N}"[..12],
        });
    }
}

// Endpoint that uses the chain
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapPost("/", async (
            OrderProcessingRequest request,
            IChainInvoker<OrderProcessingRequest, OrderProcessingResponse> chain) =>
        {
            var response = await chain.HandleAsync(request);
            if (response?.Status == "Validation failed" || response?.Status == "Invalid quantity")
                return Results.BadRequest(response.Status);
            return Results.Ok(response);
        })
        .WithSummary("Process an order")
        .Accepts<OrderProcessingRequest>("application/json")
        .Produces<OrderProcessingResponse>()
        .Produces(400);
    }
}
```

---

## Swagger / OpenAPI

With `AddEndpointsApiExplorer()` and `AddSwaggerGen()` (already in the template), all endpoints are automatically documented:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Navigate to `https://localhost:{port}/swagger` to explore the interactive API documentation.

---

## Running the Example

The repository contains a ready-to-run ASP.NET Core example:

```bash
dotnet run --project examples/web/Forma.Examples.Web.AspNetCore
```

Then open the Swagger UI at:

```
https://localhost:7xxx/swagger
```

---

## See Also

- [Forma.Mediator docs](/packages/mediator)
- [Forma.Decorator docs](/packages/decorator)
- [Forma.Chains docs](/packages/chains)
- [Console App Guide](/guides/console-app)
