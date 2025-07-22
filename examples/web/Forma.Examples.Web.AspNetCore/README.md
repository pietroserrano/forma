# Forma ASP.NET Core Web API Example

This example demonstrates how to use Forma's design patterns in an ASP.NET Core Web API application, showcasing real-world usage of the Mediator, Decorator, and Chains patterns in a web context with **Entity Framework Core and SQLite** for persistent data storage.

## What This Example Demonstrates

### 🎯 **Mediator Pattern in Web APIs**
- **CQRS Implementation**: Commands and queries handled through the mediator
- **Controller Simplification**: Controllers become thin layers that delegate to mediator
- **Request/Response Handling**: Structured approach to API request processing
- **Error Handling**: Centralized error handling and logging

### 🎨 **Decorator Pattern for Cross-Cutting Concerns**
- **Forma.Decorator Integration**: Uses official `services.Decorate<T, TDecorator>()` extensions
- **Logging Decorator**: Automatic logging of method calls, execution times, and results
- **Validation Decorator**: Input validation with detailed error messages
- **Caching Decorator**: In-memory caching with expiration for performance optimization
- **Service Enhancement**: Adding functionality without modifying core business logic

### 🔗 **Chains Pattern for Complex Workflows**
- **Order Processing Pipeline**: Validation → Inventory Check → Pricing → Order Creation
- **Payment Processing Pipeline**: Validation → Fraud Detection → Payment → Notification
- **Sequential Processing**: Each handler performs a specific step in the workflow
- **Error Handling**: Early termination when any step fails
- **Request Tracking**: Processing steps are recorded for debugging and monitoring

### 🗄️ **Entity Framework Core Integration**
- **SQLite Database**: Lightweight, file-based database for development and testing
- **Entity Models**: Properly configured entities with validation and constraints
- **Database Seeding**: Initial data for testing and demonstration
- **Automatic Migrations**: Database creation and schema management
- **Repository Pattern**: UserService as a repository with EF Core integration

## Project Structure

```
Forma.Examples.Web.AspNetCore/
├── Controllers/
│   ├── UsersController.cs       # REST API controller using mediator
│   └── OrdersController.cs      # Order processing using chains
├── Data/
│   ├── Entities/
│   │   └── User.cs              # EF Core entity models
│   └── FormaExamplesDbContext.cs # Database context with seeding
├── Handlers/
│   └── UserHandlers.cs          # Command and query handlers
├── Models/
│   └── UserModels.cs            # DTOs and request/response models
├── Services/
│   ├── UserService.cs           # Core business service with EF Core
│   └── Decorators/
│       └── UserServiceDecorators.cs  # Service decorators
├── Chains/
│   ├── OrderChainHandlers.cs    # Order processing chain handlers
│   └── PaymentChainHandlers.cs  # Payment processing chain handlers
├── Program.cs                   # DI configuration and app setup
├── appsettings.json             # Configuration with database connection
└── README.md                    # This file
```

## API Endpoints

The example provides multiple APIs demonstrating different patterns:

### **User Management API (Mediator + Decorators)**

#### **GET /api/users**
- Retrieves all users
- Uses mediator to handle `GetAllUsersQuery`
- Demonstrates decorator chain: Logging → Validation → Caching → UserService

#### **GET /api/users/{id}**
- Retrieves a specific user by ID
- Input validation through decorators
- Caching for performance optimization
- Error handling for not found scenarios

#### **POST /api/users**
- Creates a new user
- Request body validation
- Returns created user with location header
- Full decorator chain execution

#### **PUT /api/users/{id}**
- Updates an existing user
- Validates both ID and request body
- Cache invalidation and update
- Returns updated user data

#### **DELETE /api/users/{id}**
- Deletes a user by ID
- Validation and cache cleanup
- Returns 204 No Content on success

### **Order Management API (Chains Pattern)**

#### **POST /api/orders**
- Creates a new order using order processing chain
- **Chain Flow**: Validation → Inventory Check → Pricing → Order Creation
- Request body: `{ "productId": "PROD-001", "quantity": 2, "customerId": "CUST-123", "customerEmail": "customer@example.com" }`
- Returns complete order details with processing steps

#### **POST /api/orders/{orderId}/payment**
- Processes payment for an order using payment chain
- **Chain Flow**: Validation → Fraud Detection → Payment Processing → Notification
- Request body: `{ "amount": 99.99, "cardNumber": "4532-1234-5678-9012", "customerEmail": "customer@example.com" }`
- Returns payment confirmation

#### **GET /api/orders/samples**
- Returns sample data for testing the chains
- Provides example requests for both order creation and payment processing

## Pattern Implementation Details

### **Mediator Pattern**
```csharp
// Controller method
[HttpPost]
public async Task<ActionResult<UserCreatedResponse>> CreateUser([FromBody] CreateUserRequest request)
{
    var command = new CreateUserCommand(request.Name, request.Email);
    var result = await _mediator.SendAsync(command);
    return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
}

// Handler implementation
public class CreateUserCommandHandler : IHandler<CreateUserCommand, UserCreatedResponse>
{
    public async Task<UserCreatedResponse> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _userService.CreateUserAsync(request.Name, request.Email);
        return new UserCreatedResponse(user.Id, $"User '{user.Name}' created successfully");
    }
}
```

### **Decorator Pattern**
```csharp
// DI Configuration using Forma.Decorator extensions
builder.Services.AddScoped<IUserService, UserService>();

// Apply decorators using Forma.Decorator library
// Order: Inner (UserService) -> Caching -> Validation -> Logging (Outer)
builder.Services.Decorate<IUserService, CachingUserServiceDecorator>();
builder.Services.Decorate<IUserService, ValidationUserServiceDecorator>();
builder.Services.Decorate<IUserService, LoggingUserServiceDecorator>();
```

### **Chains Pattern**
```csharp
// Register chain handlers for order processing
builder.Services.AddTransient<OrderValidationHandler>();
builder.Services.AddTransient<InventoryCheckHandler>();
builder.Services.AddTransient<OrderPricingHandler>();
builder.Services.AddTransient<OrderCreationHandler>();

// Configure order processing chain
builder.Services.AddChain<OrderProcessingRequest, OrderProcessingResponse>(
    typeof(OrderValidationHandler),
    typeof(InventoryCheckHandler),
    typeof(OrderPricingHandler),
    typeof(OrderCreationHandler));
```

## Running the Example

### **Prerequisites**
- .NET 8.0 SDK or later
- Your favorite API testing tool (Postman, curl, etc.)

### **Build and Run**
```bash
# Navigate to the project directory
cd examples/web/Forma.Examples.Web.AspNetCore

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

The application will automatically:
- Create the SQLite database (`FormaExamples.db`) if it doesn't exist
- Apply any pending migrations
- Seed the database with initial user data
- Start the web server

The API will be available at:
- **HTTPS**: `https://localhost:7XXX` (check console output for exact port)
- **HTTP**: `http://localhost:5XXX`
- **Swagger UI**: `https://localhost:7XXX/swagger`

### **Database Information**
- **Database File**: `FormaExamples.db` (created in the project root)
- **Database Provider**: SQLite (Entity Framework Core)
- **Initial Data**: Two sample users are seeded automatically
- **Connection String**: Configured in `appsettings.json`

### **Testing the API**

#### User Management (Mediator + Decorators):

##### Create a user:
```bash
curl -X POST "https://localhost:7XXX/api/users" \
     -H "Content-Type: application/json" \
     -d '{"name": "John Doe", "email": "john@example.com"}'
```

##### Get all users:
```bash
curl -X GET "https://localhost:7XXX/api/users"
```

##### Get specific user:
```bash
curl -X GET "https://localhost:7XXX/api/users/1"
```

##### Update user:
```bash
curl -X PUT "https://localhost:7XXX/api/users/1" \
     -H "Content-Type: application/json" \
     -d '{"name": "Jane Doe", "email": "jane@example.com"}'
```

##### Delete user:
```bash
curl -X DELETE "https://localhost:7XXX/api/users/1"
```

#### Order Processing (Chains Pattern):

##### Create an order:
```bash
curl -X POST "https://localhost:7XXX/api/orders" \
     -H "Content-Type: application/json" \
     -d '{"productId": "PROD-001", "quantity": 2, "customerId": "CUST-123", "customerEmail": "customer@example.com"}'
```

##### Process payment for order:
```bash
curl -X POST "https://localhost:7XXX/api/orders/ORD-20250721-1234/payment" \
     -H "Content-Type: application/json" \
     -d '{"amount": 99.99, "cardNumber": "4532-1234-5678-9012", "customerEmail": "customer@example.com"}'
```

##### Get sample data:
```bash
curl -X GET "https://localhost:7XXX/api/orders/samples"
```

## Key Benefits Demonstrated

### **🏗️ Clean Architecture**
- **Separation of Concerns**: Controllers, handlers, and services have distinct responsibilities
- **Dependency Inversion**: Services depend on abstractions, not implementations
- **Single Responsibility**: Each class has one clear purpose
- **Data Persistence**: Entity Framework Core provides clean data access layer

### **🔧 Cross-Cutting Concerns**
- **Automatic Logging**: Every service call is logged with timing information
- **Input Validation**: Comprehensive validation without cluttering business logic
- **Performance Optimization**: Transparent caching improves response times
- **Database Integration**: Seamless EF Core integration with SQLite

### **🚀 Scalability Features**
- **Request Processing**: Mediator pattern enables easy addition of new operations
- **Service Enhancement**: Decorators allow adding features without changing core logic
- **Error Handling**: Consistent error responses across all endpoints
- **Database Management**: Entity Framework migrations support schema evolution

### **🧪 Testing Benefits**
- **Isolated Testing**: Each handler can be tested independently
- **Decorator Testing**: Cross-cutting concerns can be tested in isolation
- **Mock-Friendly**: Interface-based design enables easy mocking
- **Database Testing**: SQLite in-memory databases support fast unit tests

## Expected Output

When running the application, you'll see logs demonstrating the different patterns:

### **Decorator Pattern Logs:**
```
info: LoggingUserServiceDecorator[0]
      Creating user: John Doe (john@example.com)
info: ValidationUserServiceDecorator[0]
      Validating user input...
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (45ms) [Parameters=[@p0='John Doe' (Size = 100), @p1='john@example.com' (Size = 255), @p2='2025-01-21 10:30:15'], CommandType='Text', CommandTimeout='30']
      INSERT INTO "Users" ("Name", "Email", "CreatedAt")
      VALUES (@p0, @p1, @p2);
      SELECT "Id"
      FROM "Users"
      WHERE changes() = 1 AND "rowid" = last_insert_rowid();
info: UserService[0]
      User created: 3
info: LoggingUserServiceDecorator[0]
      User created successfully in 52.1ms
```

### **Chains Pattern Logs:**
```
info: OrderValidationHandler[0]
      Validating order request GUID-123
info: InventoryCheckHandler[0]
      Checking inventory for product PROD-001
info: OrderPricingHandler[0]
      Calculating pricing for order GUID-123: $51.98
info: OrderCreationHandler[0]
      Order ORD-20250721-1234 created for customer CUST-123
```

## Integration with Other Patterns

This example demonstrates how Forma patterns work together:
- **Mediator + Decorators**: User management combines CQRS with cross-cutting concerns
- **Chains for Complex Workflows**: Order and payment processing use sequential pipelines
- **Future Extensions**: Additional patterns can be easily integrated
  - **Additional Decorators**: Retry logic, circuit breakers, audit trails
  - **Advanced Mediator Features**: Pipeline behaviors, notification handling
  - **Complex Chains**: Multi-step approval workflows, business process automation
  - **Database Features**: Advanced EF Core features like change tracking, migrations, and relationships

## Database Schema

The SQLite database includes the following table:

### **Users Table**
```sql
CREATE TABLE "Users" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NULL
);

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
```

### **Sample Data**
The database is automatically seeded with:
```sql
INSERT INTO Users (Id, Name, Email, CreatedAt) VALUES 
(1, 'John Doe', 'john@example.com', '2024-01-21 10:00:00'),
(2, 'Jane Smith', 'jane@example.com', '2024-01-21 10:00:00');
```

## Performance Notes

- **Caching**: Reduces database calls for frequently accessed users
- **Validation**: Early validation prevents unnecessary processing
- **Logging**: Structured logging enables performance monitoring
- **Database Optimization**: Entity Framework Core provides efficient data access
- **Connection Pooling**: Automatic database connection management

This example provides a solid foundation for building robust ASP.NET Core APIs using Forma's design patterns.