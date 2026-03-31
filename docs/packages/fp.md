# Forma.Core.FP

**Forma.Core.FP** provides functional programming primitives for safe, composable error handling and optional values. Available as part of the `Forma.Core` package, it introduces `Result<TSuccess, TFailure>` and `Option<T>` types for railway-oriented programming.

[![NuGet](https://img.shields.io/nuget/v/Forma.Core.svg?label=Forma.Core)](https://www.nuget.org/packages/Forma.Core/)

## Installation

These types are included in `Forma.Core`:

```bash
dotnet add package Forma.Core
```

## Overview

FP primitives allow you to model operations that may fail or return no value without using exceptions or null checks:

- **`Result<TSuccess, TFailure>`** — represents an operation that either succeeds with a value or fails with an error
- **`Error`** — immutable error types for functional error handling (no exceptions!)
- **`Option<T>`** — represents a value that may or may not exist (a safer alternative to nullable types)

Both `Result` and `Option` support **fluent chaining** via `Then`, `Do`, `Validate`, and `Match` methods, enabling you to compose pipelines that short-circuit on failure or absence.

## Result<TSuccess, TFailure>

`Result<TSuccess, TFailure>` models operations that can succeed or fail with explicit error types.

### Creating Results

```csharp
using Forma.Core.FP;

// Success
var success = Result<int, string>.Success(42);

// Failure
var failure = Result<int, string>.Failure("Something went wrong");
```

### Checking Status

```csharp
if (result.IsSuccess)
{
    Console.WriteLine($"Value: {result.Value}");
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}
```

### Transforming with Then

Chain operations that depend on the previous success:

```csharp
var pipeline = ParseInt("42")
    .Then(x => Result<int, string>.Success(x * 2))
    .Then(x => Result<string, string>.Success($"Result: {x}"));

// If ParseInt fails, subsequent steps are skipped
```

### Side Effects with Do

Execute actions without transforming the result:

```csharp
var result = Result<int, string>.Success(42)
    .Do(x => Console.WriteLine($"Processing: {x}"));
```

### Validation

Ensure the value meets certain conditions:

```csharp
var validated = Result<int, string>.Success(10)
    .Validate(x => x > 5, () => "Value must be greater than 5");
```

### Pattern Matching with Match

Extract values or errors and map them to a common type:

```csharp
var message = result.Match(
    onSuccess: x => $"Success: {x}",
    onFailure: e => $"Error: {e}"
);
```

### OnSuccess and OnError

Execute side effects conditionally:

```csharp
result
    .OnSuccess(x => Console.WriteLine($"Operation succeeded with {x}"))
    .OnError(e => Console.WriteLine($"Operation failed: {e}"));
```

### Async Extensions

For async workflows, use `ThenAsync`, `DoAsync`, `ValidateAsync`, and `MatchAsync`:

```csharp
using Forma.Core.FP;

var pipeline = await FetchDataAsync()
    .ThenAsync(data => ProcessAsync(data))
    .ValidateAsync(result => IsValidAsync(result), () => "Validation failed")
    .DoAsync(result => LogAsync(result));
```

## Error Types

**Forma.Core.FP** provides a hierarchy of immutable error types as a functional alternative to exceptions. Instead of throwing exceptions, return a `Result<TSuccess, Error>` that explicitly models success or failure.

### Why Error Types?

- 🎯 **Explicit**: Errors are part of the function signature
- 🔒 **Immutable**: Error instances cannot be modified after creation
- 🚀 **Performance**: No stack traces or exception overhead
- 📦 **Serializable**: Easily serialized for APIs or logging
- 🧩 **Composable**: Pattern match and transform errors functionally

### Base Error Type

All errors inherit from the abstract `Error` record:

```csharp
public abstract record Error(string Message, string Code)
{
    public Dictionary<string, object>? Metadata { get; init; }
}
```

### Built-in Error Types

#### GenericError
Basic error with a message:
```csharp
var error = new GenericError("Something went wrong");
var error = "Message".ToError(); // Extension method
```

#### ValidationError
Validation errors with field-specific messages:
```csharp
var error = new ValidationError(
    "Validation failed",
    new Dictionary<string, string[]> 
    {
        ["Email"] = ["Email is required", "Email format is invalid"],
        ["Age"] = ["Must be 18 or older"]
    }
);

// Factory methods
var error = ("Email", "Email is required").ToValidationError();
var error = ErrorExtensions.ToValidationError(
    ("Email", "Required"),
    ("Age", "Must be 18+")
);
```

#### NotFoundError
Entity not found errors:
```csharp
var error = new NotFoundError("User", 42); 
// Message: "User with id '42' not found"

// Extension methods
var error = 42.ToNotFoundError<User>();
var error = ErrorExtensions.ToNotFoundError("Product", "ABC123");
```

#### ConflictError
Duplicate or conflict errors:
```csharp
var error = new ConflictError("Email already exists", resourceId: "user@example.com");
var error = "Email taken".ToConflictError("user@example.com");
```

#### UnauthorizedError
Authorization failures:
```csharp
var error = new UnauthorizedError(); // Default message
var error = new UnauthorizedError("Insufficient permissions");
```

#### BusinessRuleViolationError
Domain/business logic violations:
```csharp
var error = new BusinessRuleViolationError(
    "MaxOrderAmount", 
    "Order exceeds maximum allowed amount"
);

var error = ErrorExtensions.ToBusinessRuleError(
    "AccountNotActive",
    "Cannot process payment for inactive account"
);
```

#### ConcurrencyError
Optimistic concurrency violations:
```csharp
var error = new ConcurrencyError("Order", orderId);
var error = ErrorExtensions.ToConcurrencyError("Invoice", id);
```

#### DataFormatError
Invalid data format:
```csharp
var error = new DataFormatError(
    "BirthDate",
    "yyyy-MM-dd",
    "31/12/2000"
);

var error = ErrorExtensions.ToDataFormatError(
    "Price", 
    "decimal", 
    "abc"
);
```

#### ExternalServiceError
Third-party service failures:
```csharp
var error = new ExternalServiceError(
    "PaymentGateway",
    "Connection timeout",
    statusCode: 503
) { Timeout = TimeSpan.FromSeconds(30) };

var error = ErrorExtensions.ToExternalServiceError(
    "EmailService",
    "Failed to send",
    500
);
```

#### AggregateError
Multiple errors combined:
```csharp
var errors = new List<Error> 
{ 
    new ValidationError(...), 
    new BusinessRuleViolationError(...)
};
var aggregate = new AggregateError("Multiple errors occurred", errors);
var aggregate = errors.ToAggregateError();
```

### Adding Metadata

Enrich errors with additional context:

```csharp
var error = new GenericError("Database error")
    .WithMetadata("Query", "SELECT * FROM Users")
    .WithMetadata("Duration", TimeSpan.FromSeconds(5));
```

### Combining Validation Errors

```csharp
var error1 = new ValidationError("First", new Dictionary<string, string[]> 
{ 
    ["Email"] = ["Required"] 
});

var error2 = new ValidationError("Second", new Dictionary<string, string[]> 
{ 
    ["Email"] = ["Invalid format"],
    ["Password"] = ["Too short"]
});

var combined = error1.Combine(error2);
// Result: Email has both errors, Password has one
```

### Using Result with Error Types

```csharp
using Forma.Core.FP;

public Result<User, Error> CreateUser(CreateUserDto dto)
{
    // Validation
    var errors = new Dictionary<string, string[]>();
    
    if (string.IsNullOrEmpty(dto.Email))
        errors["Email"] = ["Email is required"];
    
    if (dto.Age < 18)
        errors["Age"] = ["Must be 18 or older"];
    
    if (errors.Any())
        return Result<User, Error>.Failure(
            new ValidationError("Invalid user data", errors));
    
    // Check for duplicates
    if (_repository.EmailExists(dto.Email))
        return Result<User, Error>.Failure(
            new ConflictError("Email already in use", dto.Email));
    
    // Success
    var user = new User(dto.Email, dto.Age);
    return Result<User, Error>.Success(user);
}

// Usage
var result = CreateUser(dto);
var message = result.Match(
    onSuccess: user => $"Welcome, {user.Email}!",
    onFailure: error => error switch
    {
        ValidationError ve => $"Validation failed: {string.Join(", ", ve.Errors.Keys)}",
        ConflictError ce => $"Conflict: {ce.Message}",
        NotFoundError nf => $"Not found: {nf.EntityName}",
        _ => "An error occurred"
    }
);
```

### Try Pattern for Exception Boundaries

When interfacing with code that throws exceptions, use the `Try` helpers:

```csharp
using Forma.Core.FP;

// Synchronous
var result = ResultExtensions.Try(
    () => JsonSerializer.Deserialize<User>(json),
    ex => new DataFormatError("User", "JSON", json)
);

// Asynchronous
var result = await ResultExtensions.TryAsync(
    () => httpClient.GetAsync(url),
    ex => new ExternalServiceError("API", ex.Message) 
    {
        Timeout = ex is TimeoutException ? TimeSpan.FromSeconds(30) : null
    }
);
```

### Advanced: Accumulating Validation Errors

Use `ValidateAll` to collect all validation errors instead of short-circuiting:

```csharp
var result = Result<User, Error>.Success(user)
    .ValidateAll(
        u => u.Age >= 18 
            ? Result<User, ValidationError>.Success(u)
            : Result<User, ValidationError>.Failure(
                ("Age", "Must be 18+").ToValidationError()),
        u => !string.IsNullOrEmpty(u.Email)
            ? Result<User, ValidationError>.Success(u) 
            : Result<User, ValidationError>.Failure(
                ("Email", "Required").ToValidationError())
    );
// If both fail, result contains both errors combined
```

### Recovery and Fallbacks

```csharp
// Recover with alternative value
var result = GetUser(id)
    .OrElse(User.Guest);

// Recover with function
var result = GetUser(id)
    .Recover(error => error is NotFoundError ? User.Guest : null);

// Try alternative operation
var result = GetFromCache(key)
    .OrElseTry(() => GetFromDatabase(key));
```

### Combining Multiple Results

```csharp
var result1 = GetUser(userId);
var result2 = GetOrder(orderId);
var result3 = GetPayment(paymentId);

// All must succeed, returns tuple
var combined = ResultExtensions.Combine(result1, result2, result3);
combined.Match(
    onSuccess: ((user, order, payment)) => ProcessCheckout(user, order, payment),
    onFailure: error => HandleError(error) // First error
);
```

### Async Extensions

For async workflows, use `ThenAsync`, `DoAsync`, `ValidateAsync`, and `MatchAsync`:

```csharp
using Forma.Core.FP;

var pipeline = await FetchDataAsync()
    .ThenAsync(data => ProcessAsync(data))
    .ValidateAsync(result => IsValidAsync(result), () => "Validation failed")
    .DoAsync(result => LogAsync(result));
```

## Option<T>

`Option<T>` represents a value that may or may not exist — a safer alternative to nullable types or sentinel values.

### Creating Options

```csharp
using Forma.Core.FP;

// Some (has value)
var some = Option<int>.Some(42);

// None (no value)
var none = Option<int>.None();

// From nullable
string? name = GetName();
var option = Option<string>.From(name); // Some if name != null, None otherwise
```

### Checking Presence

```csharp
if (option.IsSome)
{
    // Has value
}

if (option.IsNone)
{
    // No value
}
```

### Transforming with Then

Chain operations on the optional value:

```csharp
var transformed = ParseIntOption("42")
    .Then(x => Option<int>.Some(x * 2))
    .Then(x => Option<string>.Some($"Value: {x}"));

// If ParseIntOption returns None, subsequent steps are skipped
```

### Async Transformations

```csharp
var result = await option
    .ThenAsync(x => FetchRelatedDataAsync(x));
```

### Side Effects with Do

```csharp
option
    .Do(x => Console.WriteLine($"Value: {x}"));
```

### Async Side Effects

```csharp
await option
    .DoAsync(x => LogAsync(x));
```

### Validation

Filter based on predicates:

```csharp
var validated = Option<int>.Some(10)
    .Validate(x => x > 5); // Returns Some(10)

var invalid = Option<int>.Some(3)
    .Validate(x => x > 5); // Returns None
```

### Async Validation

```csharp
var validated = await option
    .ValidateAsync(x => IsValidAsync(x));
```

### Pattern Matching with Match

Extract the value or provide a default:

```csharp
var message = option.Match(
    some: x => $"Found: {x}",
    none: () => "Not found"
);
```

## Real-World Examples

### Parsing and Validation Pipeline (Result with Error Types)

```csharp
using Forma.Core.FP;

public Result<Order, Error> CreateOrder(string customerIdStr, string amountStr)
{
    return ParseInt(customerIdStr)
        .Then(customerId => ParseDecimal(amountStr)
            .Then(amount => ValidateAmount(amount)
                .Then(_ => CreateOrderEntity(customerId, amount))
            )
        );
}

private Result<int, Error> ParseInt(string s) =>
    int.TryParse(s, out var val)
        ? Result<int, Error>.Success(val)
        : Result<int, Error>.Failure(
            new DataFormatError("CustomerId", "integer", s));

private Result<decimal, Error> ParseDecimal(string s) =>
    decimal.TryParse(s, out var val)
        ? Result<decimal, Error>.Success(val)
        : Result<decimal, Error>.Failure(
            new DataFormatError("Amount", "decimal", s));

private Result<decimal, Error> ValidateAmount(decimal amount) =>
    amount > 0
        ? Result<decimal, Error>.Success(amount)
        : Result<decimal, Error>.Failure(
            new BusinessRuleViolationError("PositiveAmount", "Amount must be positive"));

private Result<Order, Error> CreateOrderEntity(int customerId, decimal amount)
{
    var customer = _repository.GetCustomer(customerId);
    if (customer is null)
        return Result<Order, Error>.Failure(
            customerId.ToNotFoundError<Customer>());
    
    return Result<Order, Error>.Success(new Order(customerId, amount));
}
```

### Safe Nullable Access (Option)

```csharp
public Option<Customer> FindCustomer(int id)
{
    var customer = _repository.GetById(id); // may return null
    return Option<Customer>.From(customer);
}

public string GetCustomerGreeting(int id)
{
    return FindCustomer(id)
        .Then(c => Option<string>.Some(c.Name))
        .Match(
            some: name => $"Hello, {name}!",
            none: () => "Customer not found"
        );
}
```

### Async Database Query with Validation

```csharp
using Forma.Core.FP;

public async Task<Result<User, Error>> GetActiveUserAsync(int userId)
{
    return await FetchUserAsync(userId)
        .ThenAsync(async user =>
        {
            var isActive = await CheckIsActiveAsync(user);
            return isActive
                ? Result<User, Error>.Success(user)
                : Result<User, Error>.Failure(
                    new BusinessRuleViolationError("ActiveUser", "User is not active"));
        })
        .DoAsync(user => LogAccessAsync(user));
}

private async Task<Result<User, Error>> FetchUserAsync(int id)
{
    var user = await _dbContext.Users.FindAsync(id);
    return user is not null
        ? Result<User, Error>.Success(user)
        : Result<User, Error>.Failure(id.ToNotFoundError<User>());
}
```

### Combining Result and Option

```csharp
public Result<string, string> ProcessOptionalField(Order order)
{
    return Option<string>.From(order.DiscountCode)
        .Then(code => ValidateDiscountCode(code))
        .Match(
            some: validCode => Result<string, string>.Success($"Discount: {validCode}"),
            none: () => Result<string, string>.Success("No discount applied")
        );
}

private Option<string> ValidateDiscountCode(string code)
{
    return code.Length >= 5 && code.StartsWith("DISC")
        ? Option<string>.Some(code)
        : Option<string>.None();
}
```

## Advanced Scenarios

### Form Validation with Multiple Rules

Complex validation with accumulation of errors:

```csharp
public record ValidationError(string Field, string Message);
public record UserRegistration(string Username, string Email, string Password);

public Result<UserRegistration, List<ValidationError>> ValidateRegistration(
    string username, string email, string password)
{
    var errors = new List<ValidationError>();

    // Validate username
    if (string.IsNullOrWhiteSpace(username))
        errors.Add(new ValidationError("Username", "Username is required"));
    else if (username.Length < 3)
        errors.Add(new ValidationError("Username", "Username must be at least 3 characters"));

    // Validate email
    if (string.IsNullOrWhiteSpace(email))
        errors.Add(new ValidationError("Email", "Email is required"));
    else if (!email.Contains("@"))
        errors.Add(new ValidationError("Email", "Email must be valid"));

    // Validate password
    if (string.IsNullOrWhiteSpace(password))
        errors.Add(new ValidationError("Password", "Password is required"));
    else if (password.Length < 8)
        errors.Add(new ValidationError("Password", "Password must be at least 8 characters"));

    return errors.Any()
        ? Result<UserRegistration, List<ValidationError>>.Failure(errors)
        : Result<UserRegistration, List<ValidationError>>.Success(
            new UserRegistration(username, email, password));
}

// Usage
var result = ValidateRegistration("jo", "invalid-email", "123")
    .Match(
        onSuccess: reg => $"Registration successful for {reg.Username}",
        onFailure: errors => $"Validation failed:\n{string.Join("\n", errors.Select(e => $"- {e.Field}: {e.Message}"))}"
    );
```

### File Operations with Error Handling

Safe file I/O without exceptions:

```csharp
public record FileContent(string Path, string Content);

public async Task<Result<FileContent, string>> ReadFileAsync(string path)
{
    if (string.IsNullOrWhiteSpace(path))
        return Result<FileContent, string>.Failure("Path cannot be empty");

    if (!File.Exists(path))
        return Result<FileContent, string>.Failure($"File not found: {path}");

    try
    {
        var content = await File.ReadAllTextAsync(path);
        return Result<FileContent, string>.Success(new FileContent(path, content));
    }
    catch (Exception ex)
    {
        return Result<FileContent, string>.Failure($"Error reading file: {ex.Message}");
    }
}

public async Task<Result<string, string>> ProcessConfigFileAsync(string configPath)
{
    var result = await ReadFileAsync(configPath)
        .ThenAsync(file => ParseJsonAsync(file.Content))
        .ThenAsync(config => ValidateConfigAsync(config))
        .ThenAsync(config => ApplyConfigAsync(config));
    
    return result.Match(
        onSuccess: _ => "Configuration applied successfully",
        onFailure: error => $"Configuration failed: {error}"
    );
}

private async Task<Result<JsonConfig, string>> ParseJsonAsync(string json)
{
    try
    {
        var config = JsonSerializer.Deserialize<JsonConfig>(json);
        return config != null
            ? Result<JsonConfig, string>.Success(config)
            : Result<JsonConfig, string>.Failure("JSON deserialization returned null");
    }
    catch (JsonException ex)
    {
        return Result<JsonConfig, string>.Failure($"Invalid JSON: {ex.Message}");
    }
}
```

### HTTP API Call Chain

Composing multiple API calls with error handling:

```csharp
public record ApiError(int StatusCode, string Message);
public record User(int Id, string Name);
public record UserProfile(User User, List<Post> Posts, List<Comment> Comments);

public async Task<Result<UserProfile, ApiError>> GetUserProfileAsync(int userId)
{
    return await FetchUserAsync(userId)
        .ThenAsync(async user => 
        {
            var posts = await FetchUserPostsAsync(user.Id);
            return posts.IsSuccess
                ? Result<(User, List<Post>), ApiError>.Success((user, posts.Value!))
                : Result<(User, List<Post>), ApiError>.Failure(posts.Error!);
        })
        .ThenAsync(async tuple =>
        {
            var (user, posts) = tuple;
            var comments = await FetchUserCommentsAsync(user.Id);
            return comments.IsSuccess
                ? Result<UserProfile, ApiError>.Success(new UserProfile(user, posts, comments.Value!))
                : Result<UserProfile, ApiError>.Failure(comments.Error!);
        })
        .DoAsync(profile => CacheProfileAsync(profile));
}

private async Task<Result<User, ApiError>> FetchUserAsync(int userId)
{
    var response = await _httpClient.GetAsync($"/api/users/{userId}");
    
    if (!response.IsSuccessStatusCode)
        return Result<User, ApiError>.Failure(
            new ApiError((int)response.StatusCode, "Failed to fetch user"));

    var user = await response.Content.ReadFromJsonAsync<User>();
    return user != null
        ? Result<User, ApiError>.Success(user)
        : Result<User, ApiError>.Failure(new ApiError(500, "User deserialization failed"));
}
```

### Domain-Driven Validation

Building domain entities with built-in validation:

```csharp
public record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email, string> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email, string>.Failure("Email cannot be empty");

        if (!email.Contains("@"))
            return Result<Email, string>.Failure("Email must contain @");

        if (!email.Contains("."))
            return Result<Email, string>.Failure("Email must contain a domain");

        return Result<Email, string>.Success(new Email(email));
    }
}

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money, string> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Result<Money, string>.Failure("Amount cannot be negative");

        if (string.IsNullOrWhiteSpace(currency))
            return Result<Money, string>.Failure("Currency is required");

        if (currency.Length != 3)
            return Result<Money, string>.Failure("Currency must be 3 characters (e.g., USD, EUR)");

        return Result<Money, string>.Success(new Money(amount, currency));
    }
}

// Usage in domain service
public Result<Order, string> CreateOrder(string emailStr, decimal amount, string currency)
{
    return Email.Create(emailStr)
        .Then(email => Money.Create(amount, currency)
            .Then(money => Result<Order, string>.Success(new Order(email, money)))
        );
}
```

### Transaction-like Operations

Rollback pattern with compensating actions:

```csharp
public record ReservationResult(string ReservationId);
public record PaymentResult(string TransactionId);
public record NotificationResult(string MessageId);

public async Task<Result<BookingConfirmation, string>> ProcessBookingAsync(
    BookingRequest request)
{
    string? reservationId = null;
    string? transactionId = null;

    try
    {
        var result = await CreateReservationAsync(request)
            .ThenAsync(async reservation =>
            {
                reservationId = reservation.ReservationId;
                return await ProcessPaymentAsync(request, reservation);
            })
            .ThenAsync(async payment =>
            {
                transactionId = payment.TransactionId;
                return await SendConfirmationAsync(request, payment);
            })
            .ThenAsync(notification =>
            {
                return CreateBookingConfirmationAsync(
                    reservationId!, transactionId!, notification.MessageId);
            });

        // If any step failed, rollback
        if (!result.IsSuccess && transactionId != null)
            await RefundPaymentAsync(transactionId);

        if (!result.IsSuccess && reservationId != null)
            await CancelReservationAsync(reservationId);

        return result;
    }
    catch (Exception ex)
    {
        return Result<BookingConfirmation, string>.Failure($"Unexpected error: {ex.Message}");
    }
}
```

### Nested Option Handling

Working with nested optional values:

```csharp
public record Address(string Street, string City, string? PostalCode);
public record Company(string Name, Address? Address);
public record Employee(string Name, Company? Company);

public Option<string> GetEmployeePostalCode(Employee employee)
{
    return Option<Company>.From(employee.Company)
        .Then(company => Option<Address>.From(company.Address))
        .Then(address => Option<string>.From(address.PostalCode));
}

// Usage with default value
var postalCode = GetEmployeePostalCode(employee)
    .Match(
        some: code => code,
        none: () => "Unknown"
    );
```

### Parallel Operations with Result

Running multiple independent operations and combining results:

```csharp
public record ValidationSummary(
    List<string> Errors,
    List<string> Warnings,
    bool IsValid);

public async Task<Result<ValidationSummary, string>> ValidateDataAsync(DataInput input)
{
    // Run validations in parallel
    var tasks = new[]
    {
        ValidateFormatAsync(input),
        ValidateBusinessRulesAsync(input),
        ValidateExternalConstraintsAsync(input)
    };

    var results = await Task.WhenAll(tasks);

    // Combine results
    var errors = results
        .Where(r => !r.IsSuccess)
        .Select(r => r.Error!)
        .ToList();

    var warnings = results
        .Where(r => r.IsSuccess && r.Value!.HasWarnings)
        .SelectMany(r => r.Value!.Warnings)
        .ToList();

    var isValid = !errors.Any();
    var summary = new ValidationSummary(errors, warnings, isValid);

    return isValid
        ? Result<ValidationSummary, string>.Success(summary)
        : Result<ValidationSummary, string>.Failure(
            $"Validation failed with {errors.Count} error(s)");
}
```

### Caching with Option

Implementing cache-aside pattern:

```csharp
public class CachedRepository<T> where T : class
{
    private readonly Dictionary<string, T> _cache = new();
    private readonly IRepository<T> _repository;

    public async Task<Option<T>> GetAsync(string key)
    {
        // Try cache first
        var cached = Option<T>.From(_cache.GetValueOrDefault(key));
        
        if (cached.IsSome)
            return cached.Do(item => Console.WriteLine($"Cache hit: {key}"));

        // Cache miss - fetch from repository
        var item = await _repository.GetByIdAsync(key);
        return Option<T>.From(item)
            .Do(i => 
            {
                _cache[key] = i;
                Console.WriteLine($"Cache miss: {key} - item cached");
            });
    }

    public async Task<Option<T>> GetOrFetchAsync(
        string key, 
        Func<Task<T?>> fetchFunc)
    {
        return await Option<T>.From(_cache.GetValueOrDefault(key))
            .Match(
                some: item => Task.FromResult(Option<T>.Some(item)),
                none: async () =>
                {
                    var item = await fetchFunc();
                    return Option<T>.From(item)
                        .Do(i => _cache[key] = i);
                }
            );
    }
}

// Usage
var product = await cachedRepo
    .GetOrFetchAsync("product-123", () => FetchFromDatabaseAsync("product-123"))
    .Match(
        some: p => $"Product: {p.Name}",
        none: () => "Product not found"
    );
```

## API Reference

### Result<TSuccess, TFailure>

| Method | Description |
|--------|-------------|
| `Success(value)` | Creates a successful result |
| `Failure(error)` | Creates a failed result |
| `Then<U>(func)` | Chains a transformation that returns a new Result |
| `Do(action)` | Executes a side effect if successful |
| `Validate(predicate, errorFactory)` | Validates the value; returns failure if predicate fails |
| `Match<T>(onSuccess, onFailure)` | Pattern matches to extract a value |
| `OnSuccess(action)` | Executes action only on success |
| `OnError(action)` | Executes action only on failure |

### Result Async Extensions

| Method | Description |
|--------|-------------|
| `ThenAsync<U>(func)` | Async version of `Then` |
| `DoAsync(action)` | Async version of `Do` |
| `ValidateAsync(predicate, errorFactory)` | Async validation |
| `MatchAsync<T>(onSuccess, onFailure)` | Async pattern matching |

### Option<T>

| Method | Description |
|--------|-------------|
| `Some(value)` | Creates an Option with a value |
| `None()` | Creates an Option with no value |
| `From(value)` | Creates an Option from a nullable value |
| `Then<U>(func)` | Chains a transformation that returns a new Option |
| `ThenAsync<U>(func)` | Async version of `Then` |
| `Do(action)` | Executes a side effect if Some |
| `DoAsync(action)` | Async version of `Do` |
| `Validate(predicate)` | Filters based on a predicate |
| `ValidateAsync(predicate)` | Async validation |
| `Match<T>(some, none)` | Pattern matches to extract a value or provide default |

## Design Philosophy

- **Explicit error handling** — make failure cases visible in function signatures
- **Railway-oriented programming** — operations compose like train tracks, short-circuiting on failure
- **No exceptions for control flow** — use `Result` to model expected failures
- **Null safety** — use `Option` instead of null checks
- **Async-first** — full support for async/await workflows

## Related Packages

| Package | What it adds |
|---|---|
| [Forma.Core](/packages/core) | Core abstractions including FP primitives |
| [Forma.Mediator](/packages/mediator) | Request/response mediator pattern |
| [Forma.Chains](/packages/chains) | Chain of responsibility pattern |

## Learn More

- [Console App Example](/guides/console-app) — using FP primitives in console apps
- [Forma.Examples.Console.FP](/examples/console/Forma.Examples.Console.FP) — full working example
