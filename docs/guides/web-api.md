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

## Functional Programming in Web APIs

### Result-Based Error Handling

Use `Result<T>` to handle errors without exceptions, providing clear success/failure semantics:

```csharp
using Forma.Core.FP;
using Microsoft.AspNetCore.Http.HttpResults;

public record CreateProductRequest(string Name, decimal Price, int Stock);
public record Product(int Id, string Name, decimal Price, int Stock);

public class ProductService
{
    private readonly List<Product> _products = new();
    private int _nextId = 1;

    public Result<Product> CreateProduct(string name, decimal price, int stock)
    {
        return ValidateName(name)
            .Then(_ => ValidatePrice(price))
            .Then(_ => ValidateStock(stock))
            .Then(_ =>
            {
                var product = new Product(_nextId++, name, price, stock);
                _products.Add(product);
                return Result<Product>.Success(product);
            });
    }

    public Option<Product> GetProduct(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        return Option<Product>.From(product);
    }

    private Result<string> ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<string>.Failure("Product name is required");
        if (name.Length < 3)
            return Result<string>.Failure("Product name must be at least 3 characters");
        return Result<string>.Success(name);
    }

    private Result<decimal> ValidatePrice(decimal price)
    {
        if (price <= 0)
            return Result<decimal>.Failure("Price must be greater than zero");
        if (price > 100000)
            return Result<decimal>.Failure("Price exceeds maximum allowed value");
        return Result<decimal>.Success(price);
    }

    private Result<int> ValidateStock(int stock)
    {
        if (stock < 0)
            return Result<int>.Failure("Stock cannot be negative");
        return Result<int>.Success(stock);
    }
}

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        // Create product with Result-based validation
        group.MapPost("/", (CreateProductRequest request, ProductService service) =>
        {
            var result = service.CreateProduct(request.Name, request.Price, request.Stock);
            
            return result.Match(
                onSuccess: product => Results.Created($"/api/products/{product.Id}", product),
                onFailure: error => Results.BadRequest(new { error })
            );
        })
        .WithSummary("Create a new product")
        .Produces<Product>(201)
        .Produces<object>(400);

        // Get product with Option
        group.MapGet("/{id:int}", (int id, ProductService service) =>
        {
            var result = service.GetProduct(id);
            
            return result.Match(
                onSome: product => Results.Ok(product),
                onNone: () => Results.NotFound(new { error = $"Product {id} not found" })
            );
        })
        .WithSummary("Get product by ID")
        .Produces<Product>()
        .Produces(404);
    }
}
```

### Async Operations with Result

```csharp
using Forma.Core.FP;

public class OrderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(HttpClient httpClient, ILogger<OrderService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderRequest request)
    {
        return await ValidateOrderAsync(request)
            .ThenAsync(async _ => await CheckInventoryAsync(request.ProductId, request.Quantity))
            .ThenAsync(async _ => await ProcessPaymentAsync(request.PaymentMethod, request.Amount))
            .ThenAsync(async paymentId => await SaveOrderAsync(request, paymentId))
            .DoAsync(async order => 
            {
                _logger.LogInformation("Order {OrderId} created successfully", order.Id);
                await SendConfirmationEmailAsync(request.CustomerEmail, order);
            });
    }

    private async Task<Result<Unit>> ValidateOrderAsync(CreateOrderRequest request)
    {
        if (request.Quantity <= 0)
            return Result<Unit>.Failure("Quantity must be greater than zero");
        if (request.Amount <= 0)
            return Result<Unit>.Failure("Amount must be greater than zero");
        return await Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    private async Task<Result<bool>> CheckInventoryAsync(int productId, int quantity)
    {
        try
        {
            // Simulate inventory service call
            await Task.Delay(100);
            var available = Random.Shared.Next(0, 100);
            
            return available >= quantity
                ? Result<bool>.Success(true)
                : Result<bool>.Failure($"Insufficient inventory: {available} available, {quantity} requested");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Inventory check failed: {ex.Message}");
        }
    }

    private async Task<Result<string>> ProcessPaymentAsync(string paymentMethod, decimal amount)
    {
        try
        {
            // Simulate payment processing
            await Task.Delay(200);
            var paymentId = $"PAY-{Guid.NewGuid():N}"[..16];
            return Result<string>.Success(paymentId);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Payment processing failed: {ex.Message}");
        }
    }

    private async Task<Result<OrderDto>> SaveOrderAsync(CreateOrderRequest request, string paymentId)
    {
        try
        {
            await Task.Delay(50);
            var order = new OrderDto(
                Id: Random.Shared.Next(1000, 9999),
                ProductId: request.ProductId,
                Quantity: request.Quantity,
                Amount: request.Amount,
                PaymentId: paymentId,
                Status: "Confirmed"
            );
            return Result<OrderDto>.Success(order);
        }
        catch (Exception ex)
        {
            return Result<OrderDto>.Failure($"Failed to save order: {ex.Message}");
        }
    }

    private async Task SendConfirmationEmailAsync(string email, OrderDto order)
    {
        // Send email notification
        _logger.LogInformation("Confirmation email sent to {Email}", email);
        await Task.CompletedTask;
    }
}

public record CreateOrderRequest(
    int ProductId,
    int Quantity,
    decimal Amount,
    string PaymentMethod,
    string CustomerEmail
);

public record OrderDto(
    int Id,
    int ProductId,
    int Quantity,
    decimal Amount,
    string PaymentId,
    string Status
);

public static class OrderApiEndpoints
{
    public static void MapOrderApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapPost("/", async (CreateOrderRequest request, OrderService service) =>
        {
            var result = await service.CreateOrderAsync(request);
            
            return result.Match(
                onSuccess: order => Results.Created($"/api/orders/{order.Id}", order),
                onFailure: error => Results.BadRequest(new { error })
            );
        })
        .WithSummary("Create a new order")
        .Produces<OrderDto>(201)
        .Produces<object>(400);
    }
}
```

### Option for Nullable Query Parameters

```csharp
using Forma.Core.FP;

public record SearchCriteria(string? Query, int? MinPrice, int? MaxPrice, int Page, int PageSize);
public record ProductSearchResult(List<Product> Products, int TotalCount, int Page, int TotalPages);

public class ProductSearchService
{
    public ProductSearchResult Search(SearchCriteria criteria)
    {
        var query = GetAllProducts();

        // Apply optional filters using Option
        query = Option<string>.From(criteria.Query)
            .Match(
                onSome: q => query.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)),
                onNone: () => query
            );

        query = Option<int>.From(criteria.MinPrice)
            .Match(
                onSome: min => query.Where(p => p.Price >= min),
                onNone: () => query
            );

        query = Option<int>.From(criteria.MaxPrice)
            .Match(
                onSome: max => query.Where(p => p.Price <= max),
                onNone: () => query
            );

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)criteria.PageSize);
        
        var products = query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();

        return new ProductSearchResult(products, totalCount, criteria.Page, totalPages);
    }

    private IEnumerable<Product> GetAllProducts()
    {
        // Return mock data
        return Enumerable.Range(1, 100)
            .Select(i => new Product(i, $"Product {i}", i * 10.50m, i * 5));
    }
}

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products/search", (
            string? query,
            int? minPrice,
            int? maxPrice,
            int page = 1,
            int pageSize = 10,
            ProductSearchService service) =>
        {
            var criteria = new SearchCriteria(query, minPrice, maxPrice, page, pageSize);
            var result = service.Search(criteria);
            return Results.Ok(result);
        })
        .WithSummary("Search products with optional filters")
        .Produces<ProductSearchResult>();
    }
}
```

### Integration with Mediator and Result

```csharp
using Forma.Core.FP;
using Forma.Core.Abstractions;
using Forma.Abstractions;

public record CreateUserCommand(string Email, string Password) : IRequest<Result<UserDto>>;

public class CreateUserCommandHandler : IHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailValidator _emailValidator;

    public CreateUserCommandHandler(
        IUserRepository repository,
        IPasswordHasher hasher,
        IEmailValidator emailValidator)
    {
        _repository = repository;
        _hasher = hasher;
        _emailValidator = emailValidator;
    }

    public async Task<Result<UserDto>> HandleAsync(
        CreateUserCommand request,
        CancellationToken ct = default)
    {
        return await ValidateEmail(request.Email)
            .ThenAsync(async _ => await ValidatePassword(request.Password))
            .ThenAsync(async _ => await CheckUserDoesNotExist(request.Email))
            .ThenAsync(async _ => await CreateUser(request.Email, request.Password));
    }

    private Result<string> ValidateEmail(string email)
    {
        return _emailValidator.IsValid(email)
            ? Result<string>.Success(email)
            : Result<string>.Failure("Invalid email address");
    }

    private Task<Result<string>> ValidatePassword(string password)
    {
        if (password.Length < 8)
            return Task.FromResult(Result<string>.Failure("Password must be at least 8 characters"));
        if (!password.Any(char.IsDigit))
            return Task.FromResult(Result<string>.Failure("Password must contain at least one digit"));
        return Task.FromResult(Result<string>.Success(password));
    }

    private async Task<Result<Unit>> CheckUserDoesNotExist(string email)
    {
        var exists = await _repository.ExistsAsync(email);
        return exists
            ? Result<Unit>.Failure($"User with email '{email}' already exists")
            : Result<Unit>.Success(Unit.Value);
    }

    private async Task<Result<UserDto>> CreateUser(string email, string password)
    {
        try
        {
            var hashedPassword = _hasher.Hash(password);
            var user = await _repository.CreateAsync(email, hashedPassword);
            return Result<UserDto>.Success(new UserDto(user.Id, email));
        }
        catch (Exception ex)
        {
            return Result<UserDto>.Failure($"Failed to create user: {ex.Message}");
        }
    }
}

// Endpoint
public static class UserFpEndpoints
{
    public static void MapUserFpEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users", async (
            CreateUserRequest request,
            IRequestMediator mediator) =>
        {
            var result = await mediator.SendAsync(
                new CreateUserCommand(request.Email, request.Password));
            
            return result.Match(
                onSuccess: user => Results.Created($"/api/users/{user.Id}", user),
                onFailure: error => Results.BadRequest(new { error })
            );
        })
        .WithSummary("Create user with FP-based validation");
    }
}

public record CreateUserRequest(string Email, string Password);
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
- [Forma.Core FP docs](/packages/fp)
- [Console App Guide](/guides/console-app)
