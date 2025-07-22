using Forma.Decorator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Forma.Examples.Console.Decorator;

// Program entry point demonstrating Forma Decorator pattern
public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection container
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Register base services
        services.AddTransient<IOrderService, OrderService>();
        services.AddTransient<INotificationService, EmailNotificationService>();
        services.AddTransient<IUserService, UserService>();
        
        // Apply decorators using Forma
        services.Decorate<IOrderService, LoggingOrderDecorator>();
        services.Decorate<IOrderService, ValidationOrderDecorator>();
        services.Decorate<IOrderService, CachingOrderDecorator>();
        
        services.Decorate<INotificationService, RetryNotificationDecorator>();
        services.Decorate<INotificationService, LoggingNotificationDecorator>();
        
        services.Decorate<IUserService, AuditUserDecorator>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        System.Console.WriteLine("=== Forma Decorator Pattern Example ===\n");
        System.Console.WriteLine($"Press key to continue...");
        System.Console.ReadKey();

        // Example 1: Order Service with multiple decorators
        System.Console.WriteLine("1. Testing Order Service with multiple decorators...");
        var orderService = serviceProvider.GetRequiredService<IOrderService>();
        
        try
        {
            var orderId = await orderService.CreateOrderAsync("Product A", 2, "john@example.com");
            System.Console.WriteLine($"Order created with ID: {orderId}");
        }
        catch (ArgumentException ex)
        {
            System.Console.WriteLine($"Order creation failed: {ex.Message}");
        }
        
        System.Console.WriteLine();
        
        System.Console.WriteLine($"Press key to continue...");
        System.Console.ReadKey();
        // Example 2: Order Service with invalid input (validation decorator test)
        System.Console.WriteLine("2. Testing validation decorator with invalid input...");
        try
        {
            await orderService.CreateOrderAsync("", 0, "invalid-email");
        }
        catch (ArgumentException ex)
        {
            System.Console.WriteLine($"Validation caught: {ex.Message}");
        }
        
        System.Console.WriteLine();
        System.Console.WriteLine($"Press key to continue...");
        System.Console.ReadKey();

        // Example 3: Notification Service with retry decorator
        System.Console.WriteLine("3. Testing Notification Service with retry decorator...");
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();
        
        await notificationService.SendNotificationAsync("Welcome!", "john@example.com");
        System.Console.WriteLine();
        System.Console.WriteLine($"Press key to continue...");
        System.Console.ReadKey();

        // Example 4: User Service with audit decorator
        System.Console.WriteLine("4. Testing User Service with audit decorator...");
        var userService = serviceProvider.GetRequiredService<IUserService>();
        
        var user = await userService.GetUserAsync(123);
        System.Console.WriteLine($"Retrieved user: {user.Name}");
        
        await userService.UpdateUserAsync(123, "John Updated");
        
        System.Console.WriteLine("\n=== Example completed ===");
    }
}

// Base interfaces and implementations
public interface IOrderService
{
    Task<int> CreateOrderAsync(string productName, int quantity, string customerEmail);
}

public class OrderService : IOrderService
{
    public async Task<int> CreateOrderAsync(string productName, int quantity, string customerEmail)
    {
        // Simulate order creation
        await Task.Delay(100);
        var orderId = Random.Shared.Next(1000, 9999);
        System.Console.WriteLine($"   [OrderService] Order created: {productName} x{quantity} for {customerEmail}");
        return orderId;
    }
}

public interface INotificationService
{
    Task SendNotificationAsync(string message, string email);
}

public class EmailNotificationService : INotificationService
{
    public async Task SendNotificationAsync(string message, string email)
    {
        // Simulate email sending
        await Task.Delay(50);
        System.Console.WriteLine($"   [EmailService] Email sent to {email}: {message}");
    }
}

public interface IUserService
{
    Task<User> GetUserAsync(int userId);
    Task UpdateUserAsync(int userId, string name);
}

public record User(int Id, string Name);

public class UserService : IUserService
{
    public async Task<User> GetUserAsync(int userId)
    {
        await Task.Delay(30);
        return new User(userId, "John Doe");
    }

    public async Task UpdateUserAsync(int userId, string name)
    {
        await Task.Delay(40);
        System.Console.WriteLine($"   [UserService] User {userId} updated to {name}");
    }
}

// Decorators for Order Service
public class LoggingOrderDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ILogger<LoggingOrderDecorator> _logger;

    public LoggingOrderDecorator(IOrderService inner, ILogger<LoggingOrderDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<int> CreateOrderAsync(string productName, int quantity, string customerEmail)
    {
        _logger.LogInformation("Creating order for {Product} x{Quantity} for customer {Email}", 
            productName, quantity, customerEmail);
        
        var start = DateTime.UtcNow;
        try
        {
            var result = await _inner.CreateOrderAsync(productName, quantity, customerEmail);
            var duration = DateTime.UtcNow - start;
            _logger.LogInformation("Order creation completed in {Duration}ms", duration.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order creation failed");
            throw;
        }
    }
}

public class ValidationOrderDecorator : IOrderService
{
    private readonly IOrderService _inner;

    public ValidationOrderDecorator(IOrderService inner)
    {
        _inner = inner;
    }

    public async Task<int> CreateOrderAsync(string productName, int quantity, string customerEmail)
    {
        System.Console.WriteLine("   [ValidationDecorator] Validating order parameters...");
        
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty");
            
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
            
        if (string.IsNullOrWhiteSpace(customerEmail) || !customerEmail.Contains("@"))
            throw new ArgumentException("Valid email address is required");

        System.Console.WriteLine("   [ValidationDecorator] Validation passed");
        return await _inner.CreateOrderAsync(productName, quantity, customerEmail);
    }
}

public class CachingOrderDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private static readonly Dictionary<string, int> _cache = new();

    public CachingOrderDecorator(IOrderService inner)
    {
        _inner = inner;
    }

    public async Task<int> CreateOrderAsync(string productName, int quantity, string customerEmail)
    {
        var cacheKey = $"{productName}:{quantity}:{customerEmail}";
        
        if (_cache.TryGetValue(cacheKey, out var cachedOrderId))
        {
            System.Console.WriteLine("   [CachingDecorator] Returning cached order ID");
            return cachedOrderId;
        }

        System.Console.WriteLine("   [CachingDecorator] Cache miss, creating new order");
        var orderId = await _inner.CreateOrderAsync(productName, quantity, customerEmail);
        
        _cache[cacheKey] = orderId;
        return orderId;
    }
}

// Decorators for Notification Service
public class RetryNotificationDecorator : INotificationService
{
    private readonly INotificationService _inner;
    private readonly int _maxRetries = 3;

    public RetryNotificationDecorator(INotificationService inner)
    {
        _inner = inner;
    }

    public async Task SendNotificationAsync(string message, string email)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                System.Console.WriteLine($"   [RetryDecorator] Attempt {attempt} to send notification");
                await _inner.SendNotificationAsync(message, email);
                return;
            }
            catch when (attempt < _maxRetries)
            {
                System.Console.WriteLine($"   [RetryDecorator] Attempt {attempt} failed, retrying...");
                await Task.Delay(100 * attempt); // Exponential backoff
            }
        }
    }
}

public class LoggingNotificationDecorator : INotificationService
{
    private readonly INotificationService _inner;
    private readonly ILogger<LoggingNotificationDecorator> _logger;

    public LoggingNotificationDecorator(INotificationService inner, ILogger<LoggingNotificationDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string message, string email)
    {
        _logger.LogInformation("Sending notification to {Email}", email);
        
        try
        {
            await _inner.SendNotificationAsync(message, email);
            _logger.LogInformation("Notification sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to {Email}", email);
            throw;
        }
    }
}

// Decorators for User Service
public class AuditUserDecorator : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger<AuditUserDecorator> _logger;

    public AuditUserDecorator(IUserService inner, ILogger<AuditUserDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<User> GetUserAsync(int userId)
    {
        _logger.LogInformation("Audit: User {UserId} accessed", userId);
        var result = await _inner.GetUserAsync(userId);
        System.Console.WriteLine($"   [AuditDecorator] User access logged for ID {userId}");
        return result;
    }

    public async Task UpdateUserAsync(int userId, string name)
    {
        _logger.LogInformation("Audit: User {UserId} being updated to {Name}", userId, name);
        await _inner.UpdateUserAsync(userId, name);
        System.Console.WriteLine($"   [AuditDecorator] User update logged for ID {userId}");
    }
}
