using Forma.Chains.Abstractions;
using Forma.Chains.Extensions;
using Forma.Chains.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Tests.Chains.Error;

public class ChainErrorHandlingTests
{
    [Fact]
    public async Task Handler_ThrowsException_ShouldPropagateException()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<FirstHandler>();
        services.AddTransient<ErrorThrowingHandler>();
        services.AddTransient<ThirdHandler>();
        
        services.AddChain<TestRequest>(
            typeof(FirstHandler), 
            typeof(ErrorThrowingHandler), 
            typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();
        
        var request = new TestRequest { Value = 10 };
        var results = new List<string>();
        request.Results = results;
        
        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            chain.HandleAsync(request, default));
        
        Assert.Equal("Simulated error in handler", ex.Message);
        
        // Verify that FirstHandler executed but ThirdHandler did not
        Assert.Single(results);
        Assert.Equal("FirstHandler", results[0]);
    }
    
    [Fact]
    public async Task HandlerWithResponse_ThrowsException_ShouldPropagateException()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<FirstResponseHandler>();
        services.AddTransient<ErrorThrowingResponseHandler>();
        services.AddTransient<ThirdResponseHandler>();
        
        services.AddChain<TestRequest, TestResponse>(
            typeof(FirstResponseHandler), 
            typeof(ErrorThrowingResponseHandler), 
            typeof(ThirdResponseHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest, TestResponse>>();
        
        var request = new TestRequest { Value = 15 };
        
        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            chain.HandleAsync(request, _ => throw new InvalidOperationException("No handler handled the request.")));
        
        Assert.Equal("Simulated error in response handler", ex.Message);
    }
    
    [Fact]
    public async Task Handler_WithCanceledToken_ShouldThrowOperationCanceled()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<SlowHandler>();
        
        services.AddChain<TestRequest>(typeof(SlowHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();
        
        var request = new TestRequest { Value = 10 };
        
        // Create a pre-canceled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            chain.HandleAsync(request, _ => Task.CompletedTask, cts.Token));
    }
    
    [Fact]
    public async Task ChainWithAdvancedErrorHandling_ShouldContinueOnError()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<FirstHandler>();
        services.AddTransient<ErrorRecoveringHandler>();
        services.AddTransient<ThirdHandler>();
        
        services.AddChain<TestRequest>(
            typeof(FirstHandler), 
            typeof(ErrorRecoveringHandler), 
            typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();
        
        var request = new TestRequest { Value = 10 };
        var results = new List<string>();
        request.Results = results;
        
        // Act
        await chain.HandleAsync(request, default);
        
        // Assert - Despite the error in the second handler, all handlers should execute
        Assert.Equal(3, results.Count);
        Assert.Equal("FirstHandler", results[0]);
        Assert.Equal("ErrorRecoveringHandler", results[1]);
        Assert.Equal("ThirdHandler", results[2]);
    }
}

// Handler che genera un'eccezione
public class ErrorThrowingHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulated error in handler");
    }
}

// Handler con response che genera un'eccezione
public class ErrorThrowingResponseHandler : IChainHandler<TestRequest, TestResponse>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 10 && request.Value < 20);
    }

    public Task<TestResponse> HandleAsync(TestRequest request, Func<CancellationToken, Task<TestResponse>> next, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulated error in response handler");
    }
}

// Handler che gestisce un'eccezione internamente e continua la catena
public class ErrorRecoveringHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simulazione di un'operazione che genera un'eccezione
            if (request.Value > 0)
            {
                throw new InvalidOperationException("Internal error that will be handled");
            }
        }
        catch (Exception)
        {
            // Recupero dall'errore
        }
        
        request.Results?.Add("ErrorRecoveringHandler");
        
        // Continua con il prossimo handler
        await next(cancellationToken);
    }
}

// Handler che esegue un'operazione lenta per testare la cancellazione
public class SlowHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        // Throw if already canceled
        cancellationToken.ThrowIfCancellationRequested();
        
        // Simula un'operazione lunga che controlla la cancellazione
        await Task.Delay(100, cancellationToken);
        
        await next(cancellationToken);
    }
}
