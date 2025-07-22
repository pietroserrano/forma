using Forma.Chains.Abstractions;
using Forma.Examples.Web.AspNetCore.Models;

namespace Forma.Examples.Web.AspNetCore.Chains;

/// <summary>
/// Validates the payment processing request
/// </summary>
public class PaymentValidationHandler : IChainHandler<PaymentProcessingRequest>
{
    private readonly ILogger<PaymentValidationHandler> _logger;

    public PaymentValidationHandler(ILogger<PaymentValidationHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(PaymentProcessingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task HandleAsync(
        PaymentProcessingRequest request,
        Func<CancellationToken, Task> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating payment request {RequestId}", request.RequestId);
        
        // Simulate validation delay
        await Task.Delay(30, cancellationToken);
        
        // Validate request
        if (string.IsNullOrWhiteSpace(request.OrderId))
            throw new ArgumentException("Order ID is required");
            
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");
            
        if (string.IsNullOrWhiteSpace(request.CardNumber))
            throw new ArgumentException("Card number is required");
            
        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || !request.CustomerEmail.Contains('@'))
            throw new ArgumentException("Valid customer email is required");

        request.ProcessingSteps.Add("Payment validation completed");
        _logger.LogInformation("Payment validation passed for request {RequestId}", request.RequestId);
        
        await next(cancellationToken);
    }
}

/// <summary>
/// Performs fraud detection on the payment
/// </summary>
public class PaymentFraudDetectionHandler : IChainHandler<PaymentProcessingRequest>
{
    private readonly ILogger<PaymentFraudDetectionHandler> _logger;

    public PaymentFraudDetectionHandler(ILogger<PaymentFraudDetectionHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(PaymentProcessingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task HandleAsync(
        PaymentProcessingRequest request,
        Func<CancellationToken, Task> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running fraud detection for payment {RequestId}", request.RequestId);
        
        // Simulate fraud detection delay
        await Task.Delay(150, cancellationToken);
        
        // Simple fraud detection rules
        if (request.Amount > 1000m)
        {
            _logger.LogWarning("High-value transaction detected: ${Amount}", request.Amount);
            // For demo purposes, we'll allow it but add a warning
            request.ProcessingSteps.Add($"High-value transaction flagged: ${request.Amount:F2}");
        }
        
        if (request.Amount > 5000m)
        {
            throw new InvalidOperationException("Transaction exceeds fraud detection limit");
        }
        
        // Check for suspicious card patterns
        if (request.CardNumber.StartsWith("0000") || request.CardNumber.Contains("1234"))
        {
            throw new InvalidOperationException("Suspicious card number detected");
        }

        request.ProcessingSteps.Add("Fraud detection passed");
        _logger.LogInformation("Fraud detection completed for payment {RequestId}", request.RequestId);
        
        await next(cancellationToken);
    }
}

/// <summary>
/// Processes the actual payment
/// </summary>
public class PaymentProcessingHandler : IChainHandler<PaymentProcessingRequest>
{
    private readonly ILogger<PaymentProcessingHandler> _logger;

    public PaymentProcessingHandler(ILogger<PaymentProcessingHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(PaymentProcessingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task HandleAsync(
        PaymentProcessingRequest request,
        Func<CancellationToken, Task> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payment for order {OrderId}", request.OrderId);
        
        // Simulate payment processing delay
        await Task.Delay(200, cancellationToken);
        
        // Simulate random payment failures for demonstration
        if (Random.Shared.NextDouble() < 0.1) // 10% chance of failure
        {
            throw new InvalidOperationException("Payment processing failed - insufficient funds");
        }

        var transactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        
        request.ProcessingSteps.Add($"Payment processed successfully - Transaction ID: {transactionId}");
        _logger.LogInformation("Payment processed for order {OrderId}. Amount: ${Amount:F2}, Transaction: {TransactionId}", 
            request.OrderId, request.Amount, transactionId);
        
        await next(cancellationToken);
    }
}

/// <summary>
/// Sends notification about the payment
/// </summary>
public class PaymentNotificationHandler : IChainHandler<PaymentProcessingRequest>
{
    private readonly ILogger<PaymentNotificationHandler> _logger;

    public PaymentNotificationHandler(ILogger<PaymentNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanHandleAsync(PaymentProcessingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task HandleAsync(
        PaymentProcessingRequest request,
        Func<CancellationToken, Task> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending payment notification for order {OrderId}", request.OrderId);
        
        // Simulate notification sending delay
        await Task.Delay(100, cancellationToken);
        
        // Simulate sending email notification
        var notificationId = $"NOTIF-{DateTime.UtcNow:HHmmss}-{Random.Shared.Next(10, 99)}";
        
        request.ProcessingSteps.Add($"Payment confirmation sent to {request.CustomerEmail} - {notificationId}");
        _logger.LogInformation("Payment notification sent for order {OrderId} to {CustomerEmail}", 
            request.OrderId, request.CustomerEmail);
        
        await next(cancellationToken);
    }
}