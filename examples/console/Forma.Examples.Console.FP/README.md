# Forma.Examples.Console.FP

This example demonstrates the functional programming (FP) primitives provided by `Forma.Core.FP`.

## What's Included

This console application showcases practical examples of:

### 1. **Basic Result Pipeline**
Learn how to chain operations with `Result<T>` (where failures are represented by `Error`), handling success and failure cases.

```csharp
ParseInt("42")
    .Then(MultiplyByTwo)
    .Then(ConvertToMessage)
    .OnSuccess(result => Console.WriteLine(result))
    .OnError(error => Console.WriteLine(error));
```

### 2. **Option Pipeline with Validation**
Work with `Option<T>` to handle values that may or may not exist, with built-in validation.

```csharp
ParseIntOption("25")
    .Then(x => Option<int>.Some(x * 2))
    .Validate(x => x > 10)
    .Match(
        some: result => $"Valid: {result}",
        none: () => "Invalid"
    );
```

### 3. **Form Validation**
See how to validate complex forms and accumulate multiple validation errors.

```csharp
ValidateUser("john_doe", "john@example.com", "password123")
    .Match(
        onSuccess: user => $"User validated: {user}",
        onFailure: errors => $"Errors: {string.Join(", ", errors)}"
    );
```

### 4. **Chaining Multiple Operations**
Build a simple calculator that demonstrates error propagation through a chain of operations.

```csharp
Calculate("10", "+", "5")
    .Match(
        onSuccess: result => $"Result: {result}",
        onFailure: error => $"Error: {error}"
    );
```

### 5. **Safe Dictionary Access**
Use `Option<T>` for safe dictionary lookups without null checks.

```csharp
GetSetting(settings, "host")
    .Match(
        some: value => $"Host: {value}",
        none: () => "Host not configured"
    );
```

### 6. **Combining Multiple Results**
Compose multiple validation steps when creating domain objects like orders.

```csharp
CreateOrder("john@example.com", "100.50", "USD")
    .Match(
        onSuccess: order => $"Order created: {order}",
        onFailure: error => $"Failed: {error}"
    );
```

## How to Run

From the example directory:

```bash
dotnet run
```

Or from the repository root:

```bash
dotnet run --project examples/console/Forma.Examples.Console.FP
```

## Expected Output

The application will run through all examples, showing both success and failure scenarios:

```
=== Forma.Core.FP Examples ===

1. Basic Result Pipeline
   Processing: "42"
   ✓ Success: The result is 84
   Processing: "invalid"
   ✗ Error: 'invalid' is not a valid integer

2. Option Pipeline with Validation
   Processing: "25"
   ✓ Valid value: 50
   Processing: "3"
   ✗ Value too small or invalid

...
```

## Key Concepts Demonstrated

- **Railway-Oriented Programming**: Operations that compose like train tracks, short-circuiting on failure
- **Explicit Error Handling**: Make failure cases visible in function signatures
- **Null Safety**: Use `Option<T>` instead of null checks
- **Validation Pipelines**: Chain validation rules with clear error messages
- **Fluent API**: Readable, chainable operations with `Then`, `Do`, `Validate`, and `Match`

## Learn More

- [Forma.Core.FP Documentation](/docs/packages/fp.md)
- [Forma.Core Documentation](/docs/packages/core.md)
- [Getting Started Guide](/docs/getting-started.md)
