using Forma.Chains.Abstractions;
using Forma.Chains.Extensions;
using Forma.Chains.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Tests.Chains;

public class ChainTests
{
    [Fact]
    public async Task BasicChain_ShouldHandleRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Registrazione dei servizi
        services.AddTransient<FirstHandler>();
        services.AddTransient<SecondHandler>();
        services.AddTransient<ThirdHandler>();
        
        // Registrazione della catena
        services.AddChain<TestRequest>(typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();
        
        var request = new TestRequest { Value = 10 };
        var results = new List<string>();
        request.Results = results;
        
        // Act
        await chain.HandleAsync(request, async _ => { });
        
        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("FirstHandler", results[0]);
        Assert.Equal("SecondHandler", results[1]);
        Assert.Equal("ThirdHandler", results[2]);
    }
    
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
        services.AddChain<TestRequest, TestResponse>(typeof(FirstResponseHandler), typeof(SecondResponseHandler), typeof(ThirdResponseHandler));
        
        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest, TestResponse>>();
        
        var request = new TestRequest { Value = 15 };
        
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
        services.AddChain<TestRequest, TestResponse>(
            typeof(ConditionalHandlerLessThan10),
            typeof(ConditionalHandlerBetween10And20),
            typeof(ConditionalHandlerGreaterThan20)
        );
        
        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest, TestResponse>>();
        
        // Act & Assert - Value less than 10
        var response1 = await chain.HandleAsync(new TestRequest { Value = 5 }, _ => throw new InvalidOperationException("No handler handled the request."));
        Assert.Equal("LessThan10: 5", response1.Result);
        
        // Act & Assert - Value between 10 and 20
        var response2 = await chain.HandleAsync(new TestRequest { Value = 15 }, _ => throw new InvalidOperationException("No handler handled the request."));
        Assert.Equal("Between10And20: 15", response2.Result);
        
        // Act & Assert - Value greater than 20
        var response3 = await chain.HandleAsync(new TestRequest { Value = 25 }, _ => throw new InvalidOperationException("No handler handled the request."));
        Assert.Equal("GreaterThan20: 25", response3.Result);
    }
}

// Classi di supporto per i test
public class TestRequest
{
    public int Value { get; set; }
    public List<string>? Results { get; set; }
}

public class TestResponse
{
    public string? Result { get; set; }
}

// Handler per il test base
public class FirstHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("FirstHandler");
        return next(cancellationToken);
    }
}

public class SecondHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
    
    public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("SecondHandler");
        return next(cancellationToken);
    }
}

public class ThirdHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("ThirdHandler");
        return next(cancellationToken);
    }
}

// Handler per il test con response
public class FirstResponseHandler : IChainHandler<TestRequest, TestResponse>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value < 10);
    }

    public Task<TestResponse> HandleAsync(TestRequest request, Func<CancellationToken, Task<TestResponse>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestResponse { Result = $"FirstHandler: {request.Value}" });
    }
}

public class SecondResponseHandler : IChainHandler<TestRequest, TestResponse>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 10 && request.Value <= 20);
    }

    public Task<TestResponse> HandleAsync(TestRequest request, Func<CancellationToken, Task<TestResponse>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestResponse { Result = $"SecondHandler: {request.Value}" });
    }
}

public class ThirdResponseHandler : IChainHandler<TestRequest, TestResponse>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value > 20);
    }

    public Task<TestResponse> HandleAsync(TestRequest request, Func<CancellationToken, Task<TestResponse>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestResponse { Result = $"ThirdHandler: {request.Value}" });
    }
}

// Handler per il test condizionale
public class ConditionalHandlerLessThan10 : IChainHandler<TestRequest, TestResponse>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value < 10);
    }
    
    public Task<TestResponse> HandleAsync(TestRequest request, Func<CancellationToken, Task<TestResponse>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestResponse { Result = $"LessThan10: {request.Value}" });
    }
}

public class ConditionalHandlerBetween10And20 : IChainHandler<TestRequest, TestResponse>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 10 && request.Value <= 20);
    }
    public Task<TestResponse> HandleAsync(TestRequest request, Func<CancellationToken, Task<TestResponse>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestResponse { Result = $"Between10And20: {request.Value}" });
    }
}

public class ConditionalHandlerGreaterThan20 : IChainHandler<TestRequest, TestResponse>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value > 20);
    }
    public Task<TestResponse> HandleAsync(TestRequest request, Func<CancellationToken, Task<TestResponse>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestResponse { Result = $"GreaterThan20: {request.Value}" });
    }
}
