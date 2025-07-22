using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Forma.Examples.Console.Mediator;

// Program entry point demonstrating Forma Mediator pattern
public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection container
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add Forma Mediator
        services.AddRequestMediator(config =>
        {
            // Register handlers from this assembly
            config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
            
            // Add pipeline behaviors
            config.AddRequestPreProcessor<LoggingPreProcessor>();
            config.AddRequestPostProcessor<LoggingPostProcessor>();
        });

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IRequestMediator>();
        
        System.Console.WriteLine("=== Forma Mediator Example ===\n");

        // Example 1: Simple command (no response)
        System.Console.WriteLine("1. Executing a simple command...");
        await mediator.SendAsync(new CreateUserCommand("John Doe", "john@example.com"));
        System.Console.WriteLine();
        System.Console.WriteLine($"Press key to continue...");
        System.Console.ReadKey();

        // Example 2: Query with response
        System.Console.WriteLine("2. Executing a query...");
        var user = await mediator.SendAsync(new GetUserQuery(1));
        System.Console.WriteLine($"Retrieved user: {user.Name} ({user.Email})");
        System.Console.WriteLine();
        System.Console.WriteLine($"Press key to continue...");
        System.Console.ReadKey();

        // Example 3: Command with response
        System.Console.WriteLine("3. Executing a command with response...");
        var orderId = await mediator.SendAsync(new CreateOrderCommand("Product A", 2));
        System.Console.WriteLine($"Created order with ID: {orderId}");
        System.Console.WriteLine();
        System.Console.WriteLine($"Press key to continue...");
        System.Console.ReadKey();

        // Example 4: Processing with pipeline behaviors
        System.Console.WriteLine("4. Command with validation pipeline...");
        try
        {
            await mediator.SendAsync(new ValidatedCommand(""));
        }
        catch (ArgumentException ex)
        {
            System.Console.WriteLine($"Validation error: {ex.Message}");
        }

        System.Console.WriteLine("\n=== Example completed ===");
    }
}

// Commands (no response)
public record CreateUserCommand(string Name, string Email) : IRequest;

public class CreateUserCommandHandler : IHandler<CreateUserCommand>
{
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(ILogger<CreateUserCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user: {Name} with email {Email}", request.Name, request.Email);
        
        // Simulate user creation logic
        Thread.Sleep(100); // Simulate some work
        
        System.Console.WriteLine($"✓ User '{request.Name}' created successfully!");
        return Task.CompletedTask;
    }
}

// Queries (with response)
public record GetUserQuery(int UserId) : IRequest<UserDto>;

public record UserDto(int Id, string Name, string Email);

public class GetUserQueryHandler : IHandler<GetUserQuery, UserDto>
{
    private readonly ILogger<GetUserQueryHandler> _logger;

    public GetUserQueryHandler(ILogger<GetUserQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving user with ID: {UserId}", request.UserId);
        
        // Simulate database lookup
        Thread.Sleep(50);
        
        var user = new UserDto(request.UserId, "John Doe", "john@example.com");
        return Task.FromResult(user);
    }
}

// Commands with response
public record CreateOrderCommand(string ProductName, int Quantity) : IRequest<int>;

public class CreateOrderCommandHandler : IHandler<CreateOrderCommand, int>
{
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(ILogger<CreateOrderCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task<int> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for {ProductName} (Qty: {Quantity})", request.ProductName, request.Quantity);
        
        // Simulate order creation
        Thread.Sleep(75);
        
        var orderId = Random.Shared.Next(1000, 9999);
        System.Console.WriteLine($"✓ Order created for {request.Quantity}x {request.ProductName}");
        
        return Task.FromResult(orderId);
    }
}

// Command with validation
public record ValidatedCommand(string Value) : IRequest;

public class ValidatedCommandHandler : IHandler<ValidatedCommand>
{
    public Task HandleAsync(ValidatedCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
        {
            throw new ArgumentException("Value cannot be empty", nameof(request.Value));
        }

        System.Console.WriteLine($"✓ Processing validated command with value: {request.Value}");
        return Task.CompletedTask;
    }
}

// Pipeline Behaviors - Pre and Post Processors
public class LoggingPreProcessor : IRequestPreProcessor<CreateUserCommand>, 
                                  IRequestPreProcessor<GetUserQuery>, 
                                  IRequestPreProcessor<CreateOrderCommand>,
                                  IRequestPreProcessor<ValidatedCommand>
{
    private readonly ILogger<LoggingPreProcessor> _logger;

    public LoggingPreProcessor(ILogger<LoggingPreProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(CreateUserCommand message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Pre-processing CreateUserCommand");
        return Task.CompletedTask;
    }

    public Task ProcessAsync(GetUserQuery message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Pre-processing GetUserQuery");
        return Task.CompletedTask;
    }

    public Task ProcessAsync(CreateOrderCommand message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Pre-processing CreateOrderCommand");
        return Task.CompletedTask;
    }

    public Task ProcessAsync(ValidatedCommand message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Pre-processing ValidatedCommand");
        return Task.CompletedTask;
    }
}

public class LoggingPostProcessor : IRequestPostProcessor<GetUserQuery, UserDto>,
                                   IRequestPostProcessor<CreateOrderCommand, int>
{
    private readonly ILogger<LoggingPostProcessor> _logger;

    public LoggingPostProcessor(ILogger<LoggingPostProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(GetUserQuery message, UserDto response, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Post-processing GetUserQuery -> UserDto");
        return Task.CompletedTask;
    }

    public Task ProcessAsync(CreateOrderCommand message, int response, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Post-processing CreateOrderCommand -> Order ID: {OrderId}", response);
        return Task.CompletedTask;
    }
}
