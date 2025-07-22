# Forma Chains (Pipeline) Pattern - Console Example

This example demonstrates how to use the Forma Chains pattern in a console application to implement the Chain of Responsibility pattern with pipeline processing capabilities.

## What it demonstrates

- **Chain of Responsibility**: Sequential processing through multiple handlers
- **Pipeline Processing**: Each handler can process and pass to the next in the chain
- **Conditional Processing**: Handlers can decide whether to handle a request
- **Request/Response Chains**: Support for both void and response-returning chains
- **Early Termination**: Chain can stop processing when conditions are met
- **Dependency Injection Integration**: Seamless integration with Microsoft.Extensions.DependencyInjection

## Key patterns shown

1. **Payment Processing Chain** - Multi-step payment validation and processing
2. **Order Processing Chain** - Order creation with inventory and pricing checks
3. **Validation Handler** - Input validation at the start of chains
4. **Business Logic Handlers** - Core processing steps (fraud detection, inventory checks)
5. **Notification Handler** - Final step notifications
6. **Error Handling** - Graceful failure with partial chain execution

## How to run

```bash
cd examples/console/Forma.Examples.Console.Chains
dotnet run
```

## Expected output

The example will show:
1. Complete payment processing through all chain steps
2. Order creation with response data from the chain
3. Fraud detection stopping the payment chain early

## Code structure

- **Program.cs**: Main entry point with DI setup and chain configuration
- **Request Models**: PaymentRequest, OrderRequest - define the data flowing through chains
- **Response Models**: OrderResponse - defines the output from response chains
- **Chain Handlers**: Individual processing steps implementing IChainHandler<T> or IChainHandler<T,R>
- **Chain Configuration**: Registration of handlers in the desired order

## Chain execution flow

### Payment Processing Chain
```
PaymentRequest → ValidationHandler → FraudDetectionHandler → PaymentProcessingHandler → NotificationHandler
```

### Order Processing Chain (with response)
```
OrderRequest → OrderValidationHandler → InventoryCheckHandler → PricingHandler → OrderCreationHandler → OrderResponse
```

## Handler responsibilities

- **ValidationHandler**: Validates input parameters
- **FraudDetectionHandler**: Checks for suspicious activity (can terminate chain)
- **PaymentProcessingHandler**: Processes the actual payment
- **NotificationHandler**: Sends confirmation to customer
- **OrderValidationHandler**: Validates order parameters
- **InventoryCheckHandler**: Verifies product availability
- **PricingHandler**: Calculates total amounts
- **OrderCreationHandler**: Creates the final order (terminal handler)

## Benefits of this approach

- **Single Responsibility**: Each handler has one specific task
- **Composability**: Easy to add, remove, or reorder handlers
- **Testability**: Each handler can be tested independently
- **Flexibility**: Different chains for different business processes
- **Error Isolation**: Failures are contained to specific handlers
- **Audit Trail**: Complete visibility into processing steps

## Real-world use cases

- **Order Processing**: Validation → Inventory → Pricing → Payment → Fulfillment
- **User Registration**: Validation → Duplicate Check → Account Creation → Email Verification
- **Content Moderation**: Spam Detection → Content Analysis → Approval → Publishing
- **API Request Processing**: Authentication → Rate Limiting → Validation → Business Logic
- **File Processing**: Upload → Virus Scan → Format Validation → Storage
- **Workflow Automation**: Multi-step business processes with conditional logic

## Advanced features

- **Conditional Handling**: `CanHandleAsync()` allows selective processing
- **Early Termination**: Handlers can stop the chain by throwing exceptions
- **Response Accumulation**: Data can be built up across multiple handlers
- **Async Processing**: Full support for asynchronous operations
- **Logging Integration**: Built-in logging at each step