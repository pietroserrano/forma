using Forma.Abstractions;

namespace Forma.Examples.Web.AspNetCore.Models;

// DTOs for API responses
public record UserDto(int Id, string Name, string Email, DateTime CreatedAt);
public record CreateUserRequest(string Name, string Email);
public record UserCreatedResponse(int Id, string Message);

// Commands and Queries
public record CreateUserCommand(string Name, string Email) : IRequest<UserCreatedResponse>;
public record GetUserQuery(int UserId) : IRequest<UserDto>;
public record GetAllUsersQuery : IRequest<List<UserDto>>;
public record UpdateUserCommand(int UserId, string Name, string Email) : IRequest<UserDto>;
public record DeleteUserCommand(int UserId) : IRequest;

// Orders and Chains models

/// <summary>
/// Request to create a new order
/// </summary>
public class CreateOrderRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}

/// <summary>
/// Request to process a payment
/// </summary>
public class ProcessPaymentRequest
{
    public decimal Amount { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}

/// <summary>
/// Internal order processing request used by chains
/// </summary>
public class OrderProcessingRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public List<string> ProcessingSteps { get; set; } = new();
}

/// <summary>
/// Response from order processing chain
/// </summary>
public class OrderProcessingResponse
{
    public string OrderId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> ProcessingSteps { get; set; } = new();
    public string Status { get; set; } = "Created";
}

/// <summary>
/// Internal payment processing request used by chains
/// </summary>
public class PaymentProcessingRequest
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public List<string> ProcessingSteps { get; set; } = new();
}