# Forma.Core.FP

**Forma.Core.FP** provides functional programming primitives for safe, composable error handling and optional values. Available as part of the `Forma.Core` package, it introduces `Result<T>` and `Option<T>` types for railway-oriented programming.

[![NuGet](https://img.shields.io/nuget/v/Forma.Core.svg?label=Forma.Core)](https://www.nuget.org/packages/Forma.Core/)

## Installation

These types are included in `Forma.Core`:

```bash
dotnet add package Forma.Core
```

## Overview

FP primitives allow you to model operations that may fail or return no value without using exceptions or null checks:

- **`Result<T>`** — represents an operation that either succeeds with a value or fails with an error
- **`Error`** — immutable error types for functional error handling (no exceptions!)
- **`Option<T>`** — represents a value that may or may not exist (a safer alternative to nullable types)

Both `Result` and `Option` support **fluent chaining** via `Then`, `Do`, `Validate`, and `Match` methods, enabling you to compose pipelines that short-circuit on failure or absence.

## Table of Contents

- [`Result<T>`](#resultt)
  - [Creating Results](#creating-results)
  - [Transforming, Validating, Pattern Matching](#transforming-with-then)
  - [Async Extensions](#async-extensions)
- [Error Types](#error-types)
  - [Creating Errors with Factory Methods](#creating-errors-with-factory-methods)
  - [Built-in Error Types](#built-in-error-types)
  - [When to Use Each Error Type](#when-to-use-each-error-type) 🎯
  - [Try Pattern for Exception Boundaries](#try-pattern-for-exception-boundaries)
- [`Option<T>`](#optiont)
  - [Creating Options](#creating-options)
  - [Transforming and Matching](#transforming-with-then-1)
- [Real-World Examples](#real-world-examples)
- [Advanced Scenarios](#advanced-scenarios)
- [Best Practices](#best-practices) 📖
- [API Reference](#api-reference)

## `Result<T>`

`Result<T>` models operations that can succeed or fail with explicit error types.

### Creating Results

```csharp
using Forma.Core.FP;

// Success
var success = Result<int>.Success(42);

// Failure
var failure = Result<int>.Failure(Error.Generic("Something went wrong"));
```

### Checking Status

```csharp
if (result.IsSuccess)
{
    Console.WriteLine($"Value: {result.Value}");
}
else
{
    Console.WriteLine($"Error: {result.Error.Message}");
}
```

### Transforming with Then

Chain operations that depend on the previous success:

```csharp
var pipeline = ParseInt("42")
    .Then(x => Result<int>.Success(x * 2))
    .Then(x => Result<string>.Success($"Result: {x}"));

// If ParseInt fails, subsequent steps are skipped
```

### Side Effects with Do

Execute actions without transforming the result:

```csharp
var result = Result<int>.Success(42)
    .Do(x => Console.WriteLine($"Processing: {x}"));
```

### Validation

Ensure the value meets certain conditions:

```csharp
var validated = Result<int>.Success(10)
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
    .ValidateAsync(result => IsValidAsync(result), () => Error.Generic("Validation failed"))
    .DoAsync(result => LogAsync(result));
```

## Error Types

**Forma.Core.FP** provides a hierarchy of immutable error types as a functional alternative to exceptions. Instead of throwing exceptions, return a `Result<T>` that explicitly models success or failure.

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

### Creating Errors with Factory Methods

**Forma.Core.FP** provides static factory methods on the `Error` class for creating error instances:

```csharp
using Forma.Core.FP;

// Generic errors
var error = Error.Generic("Something went wrong");

// Validation errors (single field)
var error = Error.Validation("Email", "Email is required");

// Validation errors (multiple fields)
var error = Error.Validation(
    ("Email", "Email is required"),
    ("Password", "Password must be at least 8 characters")
);

// Validation errors (from dictionary)
var errors = new Dictionary<string, string[]> 
{
    ["Email"] = ["Email is required", "Invalid format"],
    ["Age"] = ["Must be 18 or older"]
};
var error = Error.Validation(errors);

// Not found errors with type inference
var error = Error.NotFound<User>(userId);
var error = Error.NotFound("Product", "ABC123");

// Business rule violations
var error = Error.BusinessRule("MaxAmount", "Exceeds limit");

// Conflict errors
var error = Error.Conflict("Email already exists", "user@example.com");

// Concurrency errors
var error = Error.Concurrency("Order", orderId);

// Data format errors
var error = Error.DataFormat("BirthDate", "yyyy-MM-dd", "invalid");

// External service errors
var error = Error.ExternalService("PaymentAPI", "Timeout", 503);

// Aggregate multiple errors
var aggregate = Error.Aggregate(errors, "Multiple errors occurred");

// Add metadata to any error (extension method)
var enrichedError = error.WithMetadata("RequestId", "abc-123");

// Combine validation errors (extension method)
var combined = error1.Combine(error2);
```

These static factory methods provide a clean, discoverable API for error creation. They are the **recommended way** to create errors instead of using constructors directly.

### Built-in Error Types

#### GenericError
Basic error with a message:
```csharp
var error = Error.Generic("Something went wrong");
```

#### ValidationError
Validation errors with field-specific messages:
```csharp
// Single field error
var error = Error.Validation("Email", "Email is required");

// Multiple field errors
var error = Error.Validation(
    ("Email", "Email is required"),
    ("Email", "Email format is invalid"),
    ("Age", "Must be 18 or older")
);

// From dictionary
var errors = new Dictionary<string, string[]> 
{
    ["Email"] = ["Email is required", "Email format is invalid"],
    ["Age"] = ["Must be 18 or older"]
};
var error = Error.Validation(errors);
```

#### NotFoundError
Entity not found errors:
```csharp
// With generic type
var error = Error.NotFound<User>(42);
// Message: "User with id '42' not found"

// With custom entity name
var error = Error.NotFound("Product", "ABC123");
```

#### ConflictError
Duplicate or conflict errors:
```csharp
var error = Error.Conflict("Email already exists", "user@example.com");
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
var error = Error.BusinessRule(
    "MaxOrderAmount", 
    "Order exceeds maximum allowed amount"
);
```

#### ConcurrencyError
Optimistic concurrency violations:
```csharp
var error = Error.Concurrency("Order", orderId);
```

#### DataFormatError
Invalid data format:
```csharp
var error = Error.DataFormat(
    "BirthDate",
    "yyyy-MM-dd",
    "31/12/2000"
);
```

#### ExternalServiceError
Third-party service failures:
```csharp
var error = Error.ExternalService(
    "PaymentGateway",
    "Connection timeout",
    503
).WithMetadata("Timeout", TimeSpan.FromSeconds(30));
```

#### AggregateError
Multiple errors combined:
```csharp
var errors = new List<Error> 
{ 
    Error.Validation("Email", "Required"),
    Error.BusinessRule("MaxAmount", "Exceeded")
};
var aggregate = Error.Aggregate(errors, "Multiple errors occurred");
```

### Adding Metadata

Enrich errors with additional context:

```csharp
var error = Error.Generic("Database error")
    .WithMetadata("Query", "SELECT * FROM Users")
    .WithMetadata("Duration", TimeSpan.FromSeconds(5));
```

### Combining Validation Errors

```csharp
var error1 = Error.Validation("Email", "Required");

var error2 = Error.Validation(
    ("Email", "Invalid format"),
    ("Password", "Too short")
);

var combined = error1.Combine(error2);
// Result: Email has both errors, Password has one
```

## When to Use Each Error Type

Choosing the right error type makes your code more maintainable and errors easier to handle. Here's a guide:

### GenericError
**Use when:**
- The error doesn't fit any specific category
- You're wrapping unexpected exceptions
- It's a temporary placeholder during development

**Avoid when:**
- You can use a more specific error type
- The error has a well-defined category (validation, not found, etc.)

**Example scenarios:**
```csharp
// Generic database error
Error.Generic("Database connection failed")

// Unexpected state
Error.Generic("Invalid application state")
```

### ValidationError
**Use when:**
- Validating user input (forms, API requests)
- Input fails format, length, or constraint checks
- Multiple fields need validation

**Don't use for:**
- Business rule violations (use `BusinessRuleViolationError`)
- Authorization checks (use `UnauthorizedError`)

**Example scenarios:**
```csharp
// Form validation 
Error.Validation("Email", "Invalid email format")

// Multiple field validation
Error.Validation(
    ("Username", "Must be at least 3 characters"),
    ("Password", "Must contain a number")
)
```

### NotFoundError
**Use when:**
- An entity doesn't exist in the database
- A resource is not found by its identifier
- You queried for something that should exist but doesn't

**Example scenarios:**
```csharp
// Entity not found in repository
Error.NotFound<User>(userId)

// File not found
Error.NotFound("ConfigFile", "appsettings.json")
```

### ConflictError
**Use when:**
- Creating a resource that already exists (duplicate key)
- The operation violates a uniqueness constraint
- Concurrent modifications create a conflict

**Don't confuse with:**
- `ConcurrencyError` (optimistic concurrency violations)
- `BusinessRuleViolationError` (business logic violations)

**Example scenarios:**
```csharp
// Duplicate email registration
Error.Conflict("Email already registered", email)

// Username taken
Error.Conflict("Username already exists", username)
```

### UnauthorizedError
**Use when:**
- User is not authenticated
- User lacks required permissions
- Token is invalid or expired

**Don't use for:**
- Validation failures (use `ValidationError`)
- Business rule violations (use `BusinessRuleViolationError`)

**Example scenarios:**
```csharp
// Not authenticated
new UnauthorizedError()

// Missing permissions
new UnauthorizedError("Admin role required")
```

### BusinessRuleViolationError
**Use when:**
- Domain/business logic is violated
- The operation is technically valid but violates business rules
- Rules are specific to your business domain

**Don't confuse with:**
- `ValidationError` (format/constraint violations)
- `ConflictError` (uniqueness violations)

**Example scenarios:**
```csharp
// Domain rule
Error.BusinessRule("MaxOrderAmount", "Order exceeds $10,000 limit")

// Account state rule
Error.BusinessRule("AccountActive", "Cannot process payment for inactive account")

// Workflow rule
Error.BusinessRule("MinimumAge", "Must be 21 to purchase alcohol")
```

### ConcurrencyError
**Use when:**
- Optimistic concurrency check fails
- Entity was modified by another process
- Version/timestamp mismatch detected

**Don't confuse with:**
- `ConflictError` (duplicate resources)

**Example scenarios:**
```csharp
// Entity version mismatch
Error.Concurrency("Order", orderId)

// Timestamp conflict
Error.Concurrency("Invoice", invoiceId)
```

### DataFormatError
**Use when:**
- Parsing fails (JSON, XML, CSV)
- Date/time format is invalid
- Data type conversion fails

**Don't confuse with:**
- `ValidationError` (use for business input validation)

**Example scenarios:**
```csharp
// Invalid date format
Error.DataFormat("BirthDate", "yyyy-MM-dd", "32/13/2020")

// JSON parsing failed
Error.DataFormat("RequestBody", "JSON", rawData)

// Invalid number format
Error.DataFormat("Amount", "decimal", "abc.def")
```

### ExternalServiceError
**Use when:**
- Calling external APIs/services
- Third-party service is unavailable
- HTTP requests fail

**Example scenarios:**
```csharp
// Payment gateway timeout
Error.ExternalService("StripeAPI", "Request timeout", 504)

// Email service failure
Error.ExternalService("SendGrid", "Service unavailable", 503)
```

### AggregateError
**Use when:**
- Collecting multiple unrelated errors
- Running parallel validations
- Reporting all errors at once (don't short-circuit)

**Example scenarios:**
```csharp
// Multiple validation failures
var errors = new List<Error>
{
    Error.Validation("Email", "Required"),
    Error.Validation("Password", "Too weak"),
    Error.BusinessRule("TermsAccepted", "Must accept terms")
};
Error.Aggregate(errors, "Registration failed")
```

## Error Selection Flowchart

1. **Is it user input validation?** → `ValidationError`
2. **Is an entity missing?** → `NotFoundError`
3. **Is it a duplicate/uniqueness violation?** → `ConflictError`
4. **Is it an authorization issue?** → `UnauthorizedError`
5. **Is it a domain business rule?** → `BusinessRuleViolationError`
6. **Is it a concurrent modification?** → `ConcurrencyError`
7. **Is it a data format/parsing issue?** → `DataFormatError`
8. **Is it an external service failure?** → `ExternalServiceError`
9. **Do you have multiple errors?** → `AggregateError`
10. **None of the above?** → `GenericError` (consider if a more specific type could be added)

### Using Result with Error Types

```csharp
using Forma.Core.FP;

public Result<User> CreateUser(CreateUserDto dto)
{
    // Validation
    var errors = new Dictionary<string, string[]>();
    
    if (string.IsNullOrEmpty(dto.Email))
        errors["Email"] = ["Email is required"];
    
    if (dto.Age < 18)
        errors["Age"] = ["Must be 18 or older"];
    
    if (errors.Any())
        return Result<User>.Failure(
            Error.Validation(errors, "Invalid user data"));
    
    // Check for duplicates
    if (_repository.EmailExists(dto.Email))
        return Result<User>.Failure(
            Error.Conflict("Email already in use", dto.Email));
    
    // Success
    var user = new User(dto.Email, dto.Age);
    return Result<User>.Success(user);
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
    ex => Error.DataFormat("User", "JSON", json)
);

// Asynchronous
var result = await ResultExtensions.TryAsync(
    () => httpClient.GetAsync(url),
    ex => Error.ExternalService("API", ex.Message)
        .WithMetadata("Timeout", ex is TimeoutException ? TimeSpan.FromSeconds(30) : null)
);
```

### Advanced: Accumulating Validation Errors

Use `ValidateAll` to collect all validation errors instead of short-circuiting:

```csharp
var result = Result<User>.Success(user)
    .ValidateAll(
        u => u.Age >= 18 
            ? Result<User>.Success(u)
            : Result<User>.Failure(
                Error.Validation("Age", "Must be 18+")),
        u => !string.IsNullOrEmpty(u.Email)
            ? Result<User>.Success(u) 
            : Result<User>.Failure(
                Error.Validation("Email", "Required"))
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
    .ValidateAsync(result => IsValidAsync(result), () => Error.Generic("Validation failed"))
    .DoAsync(result => LogAsync(result));
```

## `Option<T>`

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

public Result<Order> CreateOrder(string customerIdStr, string amountStr)
{
    return ParseInt(customerIdStr)
        .Then(customerId => ParseDecimal(amountStr)
            .Then(amount => ValidateAmount(amount)
                .Then(_ => CreateOrderEntity(customerId, amount))
            )
        );
}

private Result<int> ParseInt(string s) =>
    int.TryParse(s, out var val)
        ? Result<int>.Success(val)
        : Result<int>.Failure(
            Error.DataFormat("CustomerId", "integer", s));

private Result<decimal> ParseDecimal(string s) =>
    decimal.TryParse(s, out var val)
        ? Result<decimal>.Success(val)
        : Result<decimal>.Failure(
            Error.DataFormat("Amount", "decimal", s));

private Result<decimal> ValidateAmount(decimal amount) =>
    amount > 0
        ? Result<decimal>.Success(amount)
        : Result<decimal>.Failure(
            Error.BusinessRule("PositiveAmount", "Amount must be positive"));

private Result<Order> CreateOrderEntity(int customerId, decimal amount)
{
    var customer = _repository.GetCustomer(customerId);
    if (customer is null)
        return Result<Order>.Failure(
            Error.NotFound<Customer>(customerId));
    
    return Result<Order>.Success(new Order(customerId, amount));
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

public async Task<Result<User>> GetActiveUserAsync(int userId)
{
    return await FetchUserAsync(userId)
        .ThenAsync(async user =>
        {
            var isActive = await CheckIsActiveAsync(user);
            return isActive
                ? Result<User>.Success(user)
                : Result<User>.Failure(
                    Error.BusinessRule("ActiveUser", "User is not active"));
        })
        .DoAsync(user => LogAccessAsync(user));
}

private async Task<Result<User>> FetchUserAsync(int id)
{
    var user = await _dbContext.Users.FindAsync(id);
    return user is not null
        ? Result<User>.Success(user)
        : Result<User>.Failure(Error.NotFound<User>(id));
}
```

### Combining Result and Option

```csharp
public Result<string> ProcessOptionalField(Order order)
{
    return Option<string>.From(order.DiscountCode)
        .Then(code => ValidateDiscountCode(code))
        .Match(
            some: validCode => Result<string>.Success($"Discount: {validCode}"),
            none: () => Result<string>.Success("No discount applied")
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

public Result<UserRegistration> ValidateRegistration(
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
        ? Result<UserRegistration>.Failure(errors)
        : Result<UserRegistration>.Success(
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

public async Task<Result<FileContent>> ReadFileAsync(string path)
{
    if (string.IsNullOrWhiteSpace(path))
        return Result<FileContent>.Failure(Error.Generic("Path cannot be empty"));

    if (!File.Exists(path))
        return Result<FileContent>.Failure(Error.Generic($"File not found: {path}"));

    try
    {
        var content = await File.ReadAllTextAsync(path);
        return Result<FileContent>.Success(new FileContent(path, content));
    }
    catch (Exception ex)
    {
        return Result<FileContent>.Failure(Error.Generic($"Error reading file: {ex.Message}"));
    }
}

public async Task<Result<string>> ProcessConfigFileAsync(string configPath)
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

private async Task<Result<JsonConfig>> ParseJsonAsync(string json)
{
    try
    {
        var config = JsonSerializer.Deserialize<JsonConfig>(json);
        return config != null
            ? Result<JsonConfig>.Success(config)
            : Result<JsonConfig>.Failure(Error.Generic("JSON deserialization returned null"));
    }
    catch (JsonException ex)
    {
        return Result<JsonConfig>.Failure(Error.DataFormat("json", "valid JSON", ex.Message));
    }
}
```

### HTTP API Call Chain

Composing multiple API calls with error handling:

```csharp
public record ApiError(int StatusCode, string Message)
    : Error(Message, $"HTTP_{StatusCode}");
public record User(int Id, string Name);
public record UserProfile(User User, List<Post> Posts, List<Comment> Comments);

public async Task<Result<UserProfile>> GetUserProfileAsync(int userId)
{
    return await FetchUserAsync(userId)
        .ThenAsync(async user => 
        {
            var posts = await FetchUserPostsAsync(user.Id);
            return posts.IsSuccess
                ? Result<(User, List<Post>)>.Success((user, posts.Value!))
                : Result<(User, List<Post>)>.Failure(posts.Error!);
        })
        .ThenAsync(async tuple =>
        {
            var (user, posts) = tuple;
            var comments = await FetchUserCommentsAsync(user.Id);
            return comments.IsSuccess
                ? Result<UserProfile>.Success(new UserProfile(user, posts, comments.Value!))
                : Result<UserProfile>.Failure(comments.Error!);
        })
        .DoAsync(profile => CacheProfileAsync(profile));
}

private async Task<Result<User>> FetchUserAsync(int userId)
{
    var response = await _httpClient.GetAsync($"/api/users/{userId}");
    
    if (!response.IsSuccessStatusCode)
        return Result<User>.Failure(
            new ApiError((int)response.StatusCode, "Failed to fetch user"));

    var user = await response.Content.ReadFromJsonAsync<User>();
    return user != null
        ? Result<User>.Success(user)
        : Result<User>.Failure(new ApiError(500, "User deserialization failed"));
}
```

### Domain-Driven Validation

Building domain entities with built-in validation:

```csharp
public record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email>.Failure(Error.Generic("Email cannot be empty"));

        if (!email.Contains("@"))
            return Result<Email>.Failure(Error.Generic("Email must contain @"));

        if (!email.Contains("."))
            return Result<Email>.Failure(Error.Generic("Email must contain a domain"));

        return Result<Email>.Success(new Email(email));
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

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Result<Money>.Failure(Error.Generic("Amount cannot be negative"));

        if (string.IsNullOrWhiteSpace(currency))
            return Result<Money>.Failure(Error.Generic("Currency is required"));

        if (currency.Length != 3)
            return Result<Money>.Failure(Error.Generic("Currency must be 3 characters (e.g., USD, EUR)"));

        return Result<Money>.Success(new Money(amount, currency));
    }
}

// Usage in domain service
public Result<Order> CreateOrder(string emailStr, decimal amount, string currency)
{
    return Email.Create(emailStr)
        .Then(email => Money.Create(amount, currency)
            .Then(money => Result<Order>.Success(new Order(email, money)))
        );
}
```

### Transaction-like Operations

Rollback pattern with compensating actions:

```csharp
public record ReservationResult(string ReservationId);
public record PaymentResult(string TransactionId);
public record NotificationResult(string MessageId);

public async Task<Result<BookingConfirmation>> ProcessBookingAsync(
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
        return Result<BookingConfirmation>.Failure(Error.Generic($"Unexpected error: {ex.Message}"));
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

public async Task<Result<ValidationSummary>> ValidateDataAsync(DataInput input)
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
        ? Result<ValidationSummary>.Success(summary)
        : Result<ValidationSummary>.Failure(
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

## Best Practices

### Error Handling Guidelines

#### 1. Be Specific with Error Types
Always prefer specific error types over `GenericError`:

```csharp
// ❌ Too generic
return Result<User>.Failure(
    Error.Generic("User not found")
);

// ✅ Specific and searchable
return Result<User>.Failure(
    Error.NotFound<User>(userId)
);
```

#### 2. Use ValidationError for Input, BusinessRuleViolationError for Logic

```csharp
// ❌ Wrong - business rule treated as validation
return Error.Validation("Amount", "Exceeds daily limit");

// ✅ Correct - clear distinction
return Error.BusinessRule("DailyLimit", "Transaction exceeds $1000 daily limit");
```

#### 3. Add Context with Metadata

```csharp
// ❌ Missing context
return Error.ExternalService("PaymentAPI", "Request failed", 500);

// ✅ Rich error context
return Error.ExternalService("PaymentAPI", "Request failed", 500)
    .WithMetadata("TransactionId", transactionId)
    .WithMetadata("Timestamp", DateTime.UtcNow)
    .WithMetadata("Endpoint", "/api/payments/process");
```

#### 4. Pattern Match for Error Handling

```csharp
// ✅ Type-safe error handling
result.Match(
    onSuccess: user => Ok(user),
    onFailure: error => error switch
    {
        ValidationError ve => BadRequest(ve.Errors),
        NotFoundError nf => NotFound(nf.Message),
        UnauthorizedError _ => Unauthorized(),
        ConflictError ce => Conflict(ce.Message),
        _ => StatusCode(500, error.Message)
    }
);
```

#### 5. Aggregate Related Errors

```csharp
// ❌ Returning first error only
if (!isValidEmail) return Error.Validation("Email", "Invalid");
if (!isValidPhone) return Error.Validation("Phone", "Invalid");

// ✅ Collect all errors
var errors = new List<Error>();
if (!isValidEmail) errors.Add(Error.Validation("Email", "Invalid"));
if (!isValidPhone) errors.Add(Error.Validation("Phone", "Invalid"));

if (errors.Any())
    return Result<User>.Failure(
        Error.Aggregate(errors, "Validation failed")
    );
```

#### 6. Don't Mix Exceptions and Errors

```csharp
// ❌ Mixing styles
try
{
    return Result<Data>.Success(Process());
}
catch (Exception ex)
{
    throw; // Don't throw after starting Result pattern
}

// ✅ Consistent error handling
return ResultExtensions.Try(
    () => Process(),
    ex => Error.Generic(ex.Message)
);
```

#### 7. Use Try/TryAsync for Exception Boundaries

```csharp
// ✅ Safe boundary for throwing code
var result = await ResultExtensions.TryAsync(
    () => _httpClient.GetFromJsonAsync<User>(url),
    ex => ex switch
    {
        HttpRequestException => Error.ExternalService("API", ex.Message),
        TimeoutException => Error.ExternalService("API", "Timeout"),
        _ => Error.Generic(ex.Message)
    }
);
```

#### 8. Name Business Rules Clearly

```csharp
// ❌ Vague rule names
Error.BusinessRule("Rule1", "Operation failed")

// ✅ Clear, searchable names
Error.BusinessRule("MinimumAccountBalance", "Account balance below $100 minimum")
Error.BusinessRule("MaxDailyWithdrawal", "Withdrawal exceeds $500 daily limit")
```

#### 9. Document Error Codes

```csharp
/// <summary>
/// Creates an order.
/// </summary>
/// <returns>
/// Success: Created order
/// Failures:
/// - ValidationError: Invalid order data
/// - BusinessRuleViolationError.MaxOrderAmount: Exceeds limit
/// - NotFoundError.Customer: Customer not found
/// - ConflictError: Order ID already exists
/// </returns>
public Result<Order> CreateOrder(OrderDto dto)
{
    // Implementation
}
```

#### 10. Keep Errors Immutable

```csharp
// ✅ Errors are immutable - use 'with' or WithMetadata
var enrichedError = baseError.WithMetadata("RequestId", requestId);

// Not: baseError.Metadata["RequestId"] = requestId; // Doesn't work with records
```

### Testing Error Scenarios

```csharp
[Fact]
public void CreateUser_WhenEmailExists_ReturnsConflictError()
{
    // Arrange
    var dto = new CreateUserDto { Email = "existing@example.com" };
    _repository.Setup(r => r.EmailExists(dto.Email)).Returns(true);

    // Act
    var result = _service.CreateUser(dto);

    // Assert
    Assert.True(result.IsFailure);
    Assert.IsType<ConflictError>(result.Error);
    Assert.Equal("Email already in use", result.Error!.Message);
}

[Fact]
public void ParseData_WhenInvalidFormat_ReturnsDataFormatError()
{
    // Arrange
    var invalidJson = "{ invalid json }";

    // Act
    var result = ResultExtensions.Try(
        () => JsonSerializer.Deserialize<Data>(invalidJson),
        ex => Error.DataFormat("Data", "JSON", invalidJson)
    );

    // Assert
    Assert.True(result.IsFailure);
    var error = Assert.IsType<DataFormatError>(result.Error);
    Assert.Equal("Data", error.FieldName);
}
```

### Anti-Patterns to Avoid

#### ❌ Using Exceptions for Control Flow
```csharp
// Bad
try
{
    var user = GetUser();
    if (user == null) throw new NotFoundException();
}
catch (NotFoundException)
{
    return NotFound();
}

// Good
return GetUserResult(userId).Match(
    onSuccess: user => Ok(user),
    onFailure: error => error switch
    {
        NotFoundError => NotFound(),
        _ => StatusCode(500)
    }
);
```

#### ❌ Ignoring Error Details
```csharp
// Bad
if (result.IsFailure)
    return Error();

// Good
if (result.IsFailure)
    return result.Error switch
    {
        ValidationError ve => BadRequest(ve.Errors),
        NotFoundError => NotFound(),
        _ => StatusCode(500, result.Error!.Message)
    };
```

#### ❌ Creating Custom Errors for Everything
```csharp
// Usually unnecessary
public record CustomPaymentError(string Reason) : Error(Reason, "PAYMENT");

// Better: use existing types with metadata
Error.ExternalService("PaymentGateway", reason)
    .WithMetadata("PaymentMethod", "CreditCard")
    .WithMetadata("Amount", amount);
```

## API Reference

### `Result<T>`

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

### `Option<T>`

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
