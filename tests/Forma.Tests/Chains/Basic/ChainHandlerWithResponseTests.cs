using Forma.Chains.Abstractions;
using Forma.Chains.Extensions;
using Forma.Chains.Implementations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forma.Tests.Chains.Basic;

// Classi di supporto per i test
public class ResponseRequest
{
    public int Value { get; set; }
    public List<string>? Results { get; set; }
}

public class ResponseResult
{
    public string? Result { get; set; }
}

public class ChainHandlerWithResponseTests
{
    [Fact]
    public async Task ChainWithResponse_ShouldHandleRequestCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Registrazione dei servizi
        services.AddTransient<FirstResponseHandler>();
        services.AddTransient<SecondResponseHandler>();
        services.AddTransient<ThirdResponseHandler>();
        
        // Registrazione della catena
        services.AddChain<ResponseRequest, ResponseResult>(typeof(FirstResponseHandler), typeof(SecondResponseHandler), typeof(ThirdResponseHandler));
        
        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<ResponseRequest, ResponseResult>>();
        
        var request = new ResponseRequest { Value = 15 };
        
        // Act
        var response = await chain.HandleAsync(request, _ => throw new InvalidOperationException("No handler handled the request."));
        
        // Assert
        Assert.Equal("SecondHandler: 15", response.Result);
    }
    
    [Fact]
    public async Task ConditionalChain_ShouldHandleRequestBasedOnCondition()
    {
        // Arrange
        var services = new ServiceCollection();
          // Registrazione dei servizi
        services.AddTransient<ConditionalHandlerLessThan10>();
        services.AddTransient<ConditionalHandlerBetween10And20>();
        services.AddTransient<ConditionalHandlerGreaterThan20>();
        
        // Registrazione della catena
        services.AddChain<ResponseRequest, ResponseResult>(
            typeof(ConditionalHandlerLessThan10),
            typeof(ConditionalHandlerBetween10And20),
            typeof(ConditionalHandlerGreaterThan20)
        );
        
        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<ResponseRequest, ResponseResult>>();
        
        // Act & Assert - Value less than 10
        var response1 = await chain.HandleAsync(new ResponseRequest { Value = 5 }, _ => throw new InvalidOperationException("No handler handled the request."));
        Assert.Equal("LessThan10: 5", response1.Result);
        
        // Act & Assert - Value between 10 and 20
        var response2 = await chain.HandleAsync(new ResponseRequest { Value = 15 }, _ => throw new InvalidOperationException("No handler handled the request."));
        Assert.Equal("Between10And20: 15", response2.Result);
        
        // Act & Assert - Value greater than 20
        var response3 = await chain.HandleAsync(new ResponseRequest { Value = 25 }, _ => throw new InvalidOperationException("No handler handled the request."));
        Assert.Equal("GreaterThan20: 25", response3.Result);
    }    [Fact]
    public async Task ChainWithResponse_AllHandlersSkipped_ShouldInvokeDefaultHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Registrazione di handler che non gestiranno la richiesta
        services.AddTransient<ConditionalHandlerLessThan10>();
        services.AddTransient<ConditionalHandlerBetween10And20>();
        services.AddTransient<ConditionalHandlerGreaterThan20>();
        
        // Registrazione della catena
        services.AddChain<ResponseRequest, ResponseResult>(
            typeof(ConditionalHandlerLessThan10),
            typeof(ConditionalHandlerBetween10And20),
            typeof(ConditionalHandlerGreaterThan20)
        );
        
        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<ResponseRequest, ResponseResult>>();
        
        var request = new ResponseRequest { Value = 100 }; // Nessun handler gestirà questo valore
        
        var defaultResponse = new ResponseResult { Result = "Default" };
        
        // Act
        var result = await chain.HandleAsync(
            request, 
            _ => Task.FromResult(defaultResponse)
        );
        
        // Assert - Should return the default response
        Assert.Same(defaultResponse, result);
        Assert.Equal("Default", result.Result);
    }    [Fact]
    public async Task ChainWithResponse_WithCancellationToken_ShouldHandleRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<FirstResponseHandler>();
        services.AddTransient<SecondResponseHandler>();
        services.AddTransient<ThirdResponseHandler>();
        
        services.AddChain<ResponseRequest, ResponseResult>(typeof(FirstResponseHandler), typeof(SecondResponseHandler), typeof(ThirdResponseHandler));
        
        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<ResponseRequest, ResponseResult>>();
        
        var request = new ResponseRequest { Value = 15 };
        
        using var cts = new CancellationTokenSource();
        
        // Act
        var response = await chain.HandleAsync(request, 
            _ => throw new InvalidOperationException("No handler handled the request."), 
            cts.Token);
        
        // Assert
        Assert.Equal("SecondHandler: 15", response.Result);
    }
}

// Handler per il test con response
public class FirstResponseHandler : IChainHandler<ResponseRequest, ResponseResult>
{
    public Task<bool> CanHandleAsync(ResponseRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value < 10);
    }

    public Task<ResponseResult> HandleAsync(ResponseRequest request, Func<CancellationToken, Task<ResponseResult>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseResult { Result = $"FirstHandler: {request.Value}" });
    }
}

public class SecondResponseHandler : IChainHandler<ResponseRequest, ResponseResult>
{
    public Task<bool> CanHandleAsync(ResponseRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 10 && request.Value < 20);
    }

    public Task<ResponseResult> HandleAsync(ResponseRequest request, Func<CancellationToken, Task<ResponseResult>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseResult { Result = $"SecondHandler: {request.Value}" });
    }
}

public class ThirdResponseHandler : IChainHandler<ResponseRequest, ResponseResult>
{
    public Task<bool> CanHandleAsync(ResponseRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 20 && request.Value < 30);
    }

    public Task<ResponseResult> HandleAsync(ResponseRequest request, Func<CancellationToken, Task<ResponseResult>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseResult { Result = $"ThirdHandler: {request.Value}" });
    }
}

// Handler per il test condizionale
public class ConditionalHandlerLessThan10 : IChainHandler<ResponseRequest, ResponseResult>
{
    public Task<bool> CanHandleAsync(ResponseRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value < 10);
    }
    
    public Task<ResponseResult> HandleAsync(ResponseRequest request, Func<CancellationToken, Task<ResponseResult>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseResult { Result = $"LessThan10: {request.Value}" });
    }
}

public class ConditionalHandlerBetween10And20 : IChainHandler<ResponseRequest, ResponseResult>
{
    public Task<bool> CanHandleAsync(ResponseRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 10 && request.Value < 20);
    }
    public Task<ResponseResult> HandleAsync(ResponseRequest request, Func<CancellationToken, Task<ResponseResult>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseResult { Result = $"Between10And20: {request.Value}" });
    }
}

public class ConditionalHandlerGreaterThan20 : IChainHandler<ResponseRequest, ResponseResult>
{
    public Task<bool> CanHandleAsync(ResponseRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 20 && request.Value < 30);
    }
    public Task<ResponseResult> HandleAsync(ResponseRequest request, Func<CancellationToken, Task<ResponseResult>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResponseResult { Result = $"GreaterThan20: {request.Value}" });
    }
}
