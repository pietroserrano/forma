# Forma Examples

This directory contains comprehensive examples demonstrating how to use Forma's design patterns in both console applications and ASP.NET Core applications.

## 📁 Directory Structure

```
examples/
├── console/                          # Console application examples
│   ├── Forma.Examples.Console.Mediator/
│   ├── Forma.Examples.Console.Decorator/
│   ├── Forma.Examples.Console.Chains/
│   └── Forma.Examples.Console.DependencyInjection/
├── aspnet-core/                      # ASP.NET Core application examples (planned)
│   ├── Forma.Examples.WebApi.Mediator/
│   ├── Forma.Examples.WebApi.Decorator/
│   ├── Forma.Examples.WebApi.Chains/
│   └── Forma.Examples.WebApi.Complete/
├── Forma.Core/                       # Core abstractions (local copy)
├── Forma.Mediator/                   # Mediator implementation (local copy)
├── Forma.Decorator/                  # Decorator implementation (local copy)
├── Forma.Chains/                     # Chains implementation (local copy)
└── Forma.Examples.sln                # Solution file for all examples
```

## 🚀 Quick Start

### Build all examples
```bash
cd examples
dotnet build
```

### Run a specific example
```bash
cd examples/console/Forma.Examples.Console.Mediator
dotnet run
```

## 📚 Examples Overview

### 1. Mediator Pattern Examples

#### Console: [Forma.Examples.Console.Mediator](./console/Forma.Examples.Console.Mediator/)
- **Purpose**: Demonstrates CQRS (Command Query Responsibility Segregation) pattern
- **Features**: Commands, Queries, Pipeline behaviors, Validation
- **Use Cases**: User management, Order processing, Request/Response handling

```csharp
// Command without response
await mediator.SendAsync(new CreateUserCommand("John", "john@example.com"));

// Query with response
var user = await mediator.SendAsync(new GetUserQuery(1));

// Command with response
var orderId = await mediator.SendAsync(new CreateOrderCommand("Product", 2));
```

### 2. Decorator Pattern Examples

#### Console: [Forma.Examples.Console.Decorator](./console/Forma.Examples.Console.Decorator/)
- **Purpose**: Shows how to add cross-cutting concerns without modifying existing code
- **Features**: Logging, Validation, Caching, Retry logic, Auditing
- **Use Cases**: Service enhancement, AOP (Aspect-Oriented Programming)

```csharp
// Automatic decoration through DI
services.Decorate<IOrderService, LoggingOrderDecorator>();
services.Decorate<IOrderService, ValidationOrderDecorator>();
services.Decorate<IOrderService, CachingOrderDecorator>();
```

### 3. Chains (Pipeline) Pattern Examples

#### Console: [Forma.Examples.Console.Chains](./console/Forma.Examples.Console.Chains/)
- **Purpose**: Implements Chain of Responsibility pattern for sequential processing
- **Features**: Pipeline processing, Conditional handling, Early termination
- **Use Cases**: Payment processing, Order fulfillment, Content moderation

```csharp
// Chain configuration
services.AddChain<PaymentRequest>(
    typeof(ValidationHandler),
    typeof(FraudDetectionHandler),
    typeof(PaymentProcessingHandler),
    typeof(NotificationHandler));
```

### 4. Complete Integration Example

#### Console: [Forma.Examples.Console.DependencyInjection](./console/Forma.Examples.Console.DependencyInjection/)
- **Purpose**: Shows all patterns working together in a real-world scenario
- **Features**: E-commerce application, Pattern composition, Service decoration
- **Use Cases**: Complex business workflows, Enterprise applications

```csharp
// Patterns working together
ProcessOrderCommand → Mediator → Chain → Decorated Services
```

## 🎯 Pattern Usage Guide

### When to Use Mediator Pattern
- ✅ Request/Response scenarios
- ✅ CQRS implementations
- ✅ Decoupling request senders from handlers
- ✅ Pipeline behaviors (logging, validation, caching)

### When to Use Decorator Pattern
- ✅ Adding cross-cutting concerns
- ✅ Service enhancement without modification
- ✅ Aspect-oriented programming
- ✅ Composable behaviors

### When to Use Chains Pattern
- ✅ Sequential processing workflows
- ✅ Conditional handler execution
- ✅ Pipeline processing with multiple steps
- ✅ Chain of responsibility scenarios

### Pattern Combinations
- **Mediator + Chains**: Command handlers that execute business workflows
- **Mediator + Decorators**: Enhanced handlers with cross-cutting concerns
- **Chains + Decorators**: Pipeline steps with automatic service enhancement
- **All Together**: Complex applications with multiple concerns

## 🛠️ Development Setup

### Prerequisites
- .NET 8.0 SDK or higher
- Visual Studio 2022, VS Code, or JetBrains Rider

### Local Development
```bash
# Clone the repository
git clone https://github.com/pietroserrano/forma.git
cd forma/examples

# Restore packages
dotnet restore

# Build all examples
dotnet build

# Run tests (if available)
dotnet test
```

### Project Structure
Each example follows a consistent structure:
- `Program.cs` - Main entry point and DI configuration
- `README.md` - Detailed explanation and usage guide
- Pattern-specific folders for organization
- Comprehensive inline documentation

## 📖 Learning Path

### Beginner
1. Start with **Mediator Console Example** - Learn basic CQRS patterns
2. Explore **Decorator Console Example** - Understand service enhancement
3. Review **Chains Console Example** - Grasp pipeline processing

### Intermediate
4. Study **Complete Integration Example** - See patterns working together
5. Analyze service registration and DI configuration
6. Understand pattern composition strategies

### Advanced
7. Create custom pipeline behaviors
8. Implement complex chain handlers
9. Design decorator combinations
10. Build real-world applications using multiple patterns

## 🎨 Customization Examples

### Custom Mediator Behavior
```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, PipelineBehaviorDelegate<TResponse> next)
    {
        // Custom caching logic
        return await next();
    }
}
```

### Custom Decorator
```csharp
public class MetricsDecorator<T> : T where T : class
{
    private readonly T _inner;
    private readonly IMetrics _metrics;
    
    public MetricsDecorator(T inner, IMetrics metrics)
    {
        _inner = inner;
        _metrics = metrics;
    }
    
    // Intercept method calls and add metrics
}
```

### Custom Chain Handler
```csharp
public class CustomValidationHandler : IChainHandler<MyRequest, MyResponse>
{
    public async Task<bool> CanHandleAsync(MyRequest request, CancellationToken cancellationToken)
    {
        return request.RequiresValidation;
    }
    
    public async Task<MyResponse> HandleAsync(MyRequest request, Func<CancellationToken, Task<MyResponse?>> next, CancellationToken cancellationToken)
    {
        // Custom validation logic
        return await next(cancellationToken);
    }
}
```

## 🔍 Performance Considerations

### Mediator Performance
- Handlers are resolved from DI container
- Minimal overhead for request routing
- Pipeline behaviors add sequential overhead

### Decorator Performance  
- Direct method interception
- Cached constructor information
- Optimized service resolution

### Chains Performance
- Sequential handler execution
- Early termination support
- Conditional handler skipping

## 🐛 Troubleshooting

### Common Issues

1. **Handler Not Found**
   ```
   Solution: Ensure handler is registered in DI container
   services.AddScoped<IHandler<MyRequest>, MyHandler>();
   ```

2. **Decorator Not Applied**
   ```
   Solution: Check decorator registration order
   services.Decorate<IService, OuterDecorator>();
   services.Decorate<IService, InnerDecorator>();
   ```

3. **Chain Handler Skipped**
   ```
   Solution: Verify CanHandleAsync returns true
   public Task<bool> CanHandleAsync(...) => Task.FromResult(true);
   ```

### Debugging Tips
- Enable detailed logging to trace execution
- Use breakpoints in handlers to verify calls
- Check DI container registrations
- Validate request/response types match

## 📝 Contributing

To add new examples:
1. Follow the established project structure
2. Include comprehensive README documentation
3. Add inline code comments explaining key concepts
4. Ensure examples build and run successfully
5. Update this main README with new example information

## 📚 Additional Resources

- [Forma Repository](https://github.com/pietroserrano/forma)
- [Microsoft Dependency Injection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Decorator Pattern](https://refactoring.guru/design-patterns/decorator)
- [Chain of Responsibility](https://refactoring.guru/design-patterns/chain-of-responsibility)