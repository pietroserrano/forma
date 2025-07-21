# Forma Decorator Pattern - Console Example

This example demonstrates how to use the Forma Decorator pattern in a console application to add cross-cutting concerns to services without modifying their original implementation.

## What it demonstrates

- **Service Decoration**: Wrapping existing services with additional behavior
- **Multiple Decorators**: Chaining multiple decorators on the same service
- **Cross-cutting Concerns**: Logging, validation, caching, retry logic, and auditing
- **Dependency Injection Integration**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **Transparent Enhancement**: Adding features without changing existing code

## Key patterns shown

1. **Logging Decorator** - Automatic logging of method calls and performance metrics
2. **Validation Decorator** - Input validation before executing business logic
3. **Caching Decorator** - Simple caching mechanism to avoid duplicate operations
4. **Retry Decorator** - Automatic retry logic for transient failures
5. **Audit Decorator** - Security and compliance logging for sensitive operations

## How to run

```bash
cd examples/console/Forma.Examples.Console.Decorator
dotnet run
```

## Expected output

The example will show:
1. Order creation with validation, logging, and caching decorators
2. Validation error handling when invalid input is provided
3. Notification service with retry and logging decorators
4. User service operations with audit logging

## Code structure

- **Program.cs**: Main entry point with DI setup and service registration
- **Base Services**: Core business logic implementations
- **Decorators**: Classes that enhance base services with additional behavior
- **Service Interfaces**: Contracts that both base services and decorators implement

## Decorator chain order

When multiple decorators are applied to a service, they form a chain:

```
Client → CachingDecorator → ValidationDecorator → LoggingDecorator → OrderService
```

The last decorator registered becomes the outermost decorator in the chain.

## Benefits of this approach

- **Single Responsibility**: Each decorator has one specific concern
- **Open/Closed Principle**: Add new behaviors without modifying existing code
- **Composition**: Combine multiple behaviors flexibly
- **Testability**: Each decorator can be tested independently
- **Configuration**: Enable/disable decorators through DI registration
- **Performance**: Selective application of cross-cutting concerns

## Real-world use cases

- **API Rate Limiting**: Control request frequency
- **Circuit Breaker**: Handle service failures gracefully
- **Metrics Collection**: Gather performance data
- **Security**: Authentication and authorization checks
- **Transformation**: Data format conversion
- **Compression**: Reduce payload sizes