# Forma Complete Integration - Console Example

This example demonstrates how all Forma patterns work together in a real-world e-commerce application, showcasing the power of combining Mediator, Decorator, Chains, and Dependency Injection patterns.

## What it demonstrates

- **Pattern Integration**: All Forma patterns working together seamlessly
- **Dependency Injection**: Proper service registration and resolution
- **Hosted Services**: Using Microsoft.Extensions.Hosting for application lifecycle
- **Real-world Scenario**: Complete e-commerce order processing flow
- **Cross-cutting Concerns**: Logging, security, auditing, and retry logic
- **Service Composition**: Services using other services through different patterns

## Architecture Overview

```
ECommerceApplication
├── Mediator Pattern
│   ├── CreateCustomerCommand → CreateCustomerHandler
│   ├── GetCustomerQuery → GetCustomerHandler
│   └── ProcessOrderCommand → ProcessOrderHandler (uses Chains)
├── Decorator Pattern
│   ├── EmailService → RetryEmailDecorator → LoggingEmailDecorator
│   └── PaymentService → SecurityPaymentDecorator → AuditPaymentDecorator
└── Chains Pattern
    └── OrderProcessingChain
        ├── OrderValidationChainHandler
        ├── InventoryChainHandler
        ├── PaymentChainHandler (uses decorated PaymentService)
        └── FulfillmentChainHandler (uses decorated EmailService)
```

## Patterns Integration Flow

1. **Mediator** receives ProcessOrderCommand
2. **ProcessOrderHandler** creates order and invokes **Chain**
3. **Chain** processes through multiple handlers:
   - Validation → Inventory Check → Payment → Fulfillment
4. **Chain handlers** use **decorated services** for cross-cutting concerns
5. **Decorators** automatically add logging, security, retry, and audit capabilities

## How to run

```bash
cd examples/console/Forma.Examples.Console.DependencyInjection
dotnet run
```

## Expected output

The example will show:
1. Customer creation via Mediator pattern
2. Order processing that combines Mediator and Chains
3. Direct chain execution for fulfillment
4. Automatic decoration effects (logging, security, auditing)

## Key Benefits Demonstrated

### 1. **Separation of Concerns**
- Each pattern handles specific responsibilities
- Cross-cutting concerns are handled by decorators
- Business logic is clean and focused

### 2. **Composability**
- Patterns can be combined and nested
- Services can use multiple patterns simultaneously
- Easy to add new behaviors without changing existing code

### 3. **Testability**
- Each component can be tested in isolation
- Dependencies are injected and can be mocked
- Clear boundaries between different concerns

### 4. **Maintainability**
- Adding new features requires minimal changes
- Patterns provide consistent structure
- Configuration is centralized in DI setup

### 5. **Performance**
- Patterns are optimized for performance
- Minimal overhead for pattern usage
- Efficient service resolution and execution

## Service Registration Strategy

### Core Services
```csharp
services.AddScoped<ICustomerService, CustomerService>();
services.AddScoped<IOrderService, OrderService>();
// ... other business services
```

### Mediator Configuration
```csharp
services.AddRequestMediator(config =>
{
    config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
    config.AddRequestPreProcessor<LoggingPreProcessor>();
    config.AddRequestPostProcessor<MetricsPostProcessor>();
});
```

### Decorator Application
```csharp
services.Decorate<IEmailService, RetryEmailDecorator>();
services.Decorate<IEmailService, LoggingEmailDecorator>();
```

### Chain Configuration
```csharp
services.AddChain<OrderProcessingRequest, OrderProcessingResponse>(
    typeof(OrderValidationChainHandler),
    typeof(InventoryChainHandler),
    // ... other handlers
);
```

## Real-world Applications

This pattern combination is ideal for:

- **E-commerce Platforms**: Order processing, payment handling, inventory management
- **Banking Systems**: Transaction processing, fraud detection, compliance checking
- **Content Management**: Publishing workflows, approval processes, content validation
- **API Gateways**: Request routing, authentication, rate limiting, logging
- **Microservices**: Service orchestration, cross-cutting concerns, business workflows
- **Event Processing**: Message handling, transformation, routing, persistence

## Extension Points

The example can be extended with:

- **Additional Decorators**: Caching, rate limiting, circuit breakers
- **More Chain Handlers**: Tax calculation, shipping, promotions
- **Extra Mediator Handlers**: Customer updates, product management
- **Pipeline Behaviors**: Validation, authorization, metrics collection
- **Background Services**: Order status updates, email notifications
- **External Integrations**: Payment gateways, shipping providers, inventory systems

## Best Practices Demonstrated

1. **Single Responsibility**: Each service/handler has one clear purpose
2. **Dependency Inversion**: All dependencies are abstracted through interfaces
3. **Open/Closed Principle**: New functionality added through composition
4. **Configuration Over Code**: Behavior controlled through DI registration
5. **Async/Await**: Proper asynchronous programming throughout
6. **Logging**: Comprehensive logging at appropriate levels
7. **Error Handling**: Graceful handling of exceptions and failures