# Forma Mediator Pattern - Console Example

This example demonstrates how to use the Forma Mediator pattern in a console application to implement clean request/response handling with the Command Query Responsibility Segregation (CQRS) pattern.

## What it demonstrates

- **Commands**: Operations that change state (CreateUserCommand, CreateOrderCommand)
- **Queries**: Operations that retrieve data (GetUserQuery)
- **Request/Response pattern**: Commands with return values
- **Pipeline behaviors**: Pre and post processors for cross-cutting concerns
- **Dependency injection**: Integration with Microsoft.Extensions.DependencyInjection
- **Logging**: Integration with Microsoft.Extensions.Logging

## Key patterns shown

1. **Simple Commands** - Fire-and-forget operations
2. **Queries** - Read operations that return data
3. **Commands with Response** - Operations that return a result
4. **Validation** - Error handling in command handlers
5. **Pipeline Processing** - Automatic logging via pre/post processors

## How to run

```bash
cd examples/console/Forma.Examples.Console.Mediator
dotnet run
```

## Expected output

The example will show:
1. Creating a user (command without response)
2. Retrieving user data (query with response)
3. Creating an order (command with response)
4. Validation error handling

## Code structure

- **Program.cs**: Main entry point with DI setup and example execution
- **Commands**: Request models that represent operations
- **Handlers**: Classes that process specific commands/queries
- **Pipeline Behaviors**: Pre/post processors for cross-cutting concerns
- **DTOs**: Data transfer objects for responses

## Benefits of this approach

- **Separation of concerns**: Each handler has a single responsibility
- **Testability**: Handlers can be tested in isolation
- **Pipeline extensibility**: Add behaviors without modifying handlers
- **Consistent patterns**: Uniform approach to handling requests
- **Dependency injection**: Leverages .NET's built-in DI container