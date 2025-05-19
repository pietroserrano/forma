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
public class TestRequest
{
    public int Value { get; set; }
    public List<string>? Results { get; set; }
}

public class TestResponse
{
    public string? Result { get; set; }
}

// Handler per i test base
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

public class ChainHandlerTests
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
    public async Task BasicChain_WithCancellationToken_ShouldHandleRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<FirstHandler>();
        services.AddTransient<SecondHandler>();
        services.AddTransient<ThirdHandler>();
        
        services.AddChain<TestRequest>(typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();
        
        var request = new TestRequest { Value = 10 };
        var results = new List<string>();
        request.Results = results;
        
        using var cts = new CancellationTokenSource();
        
        // Act
        await chain.HandleAsync(request, async _ => { }, cts.Token);
        
        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("FirstHandler", results[0]);
        Assert.Equal("SecondHandler", results[1]);
        Assert.Equal("ThirdHandler", results[2]);
    }

    [Fact]
    public async Task BasicChain_WithSpecificHandlerMatchingCondition_ShouldHandleRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<ConditionalFirstHandler>();
        services.AddTransient<ConditionalSecondHandler>();
        services.AddTransient<ConditionalThirdHandler>();
        
        services.AddChain<TestRequest>(
            typeof(ConditionalFirstHandler), 
            typeof(ConditionalSecondHandler), 
            typeof(ConditionalThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();
        
        var request = new TestRequest { Value = 15 };
        var results = new List<string>();
        request.Results = results;
        
        // Act
        await chain.HandleAsync(request, async _ => { });
        
        // Assert - Only SecondHandler should process the request
        Assert.Single(results);
        Assert.Equal("ConditionalSecondHandler", results[0]);
    }

    [Fact]
    public async Task BasicChain_WithNoHandlerMatchingCondition_ShouldInvokeDefaultHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<ConditionalFirstHandler>();
        services.AddTransient<ConditionalSecondHandler>();
        services.AddTransient<ConditionalThirdHandler>();
        
        services.AddChain<TestRequest>(
            typeof(ConditionalFirstHandler), 
            typeof(ConditionalSecondHandler), 
            typeof(ConditionalThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();
        
        var request = new TestRequest { Value = 50 };
        var results = new List<string>();
        request.Results = results;
        
        bool defaultHandlerInvoked = false;
        
        // Act
        await chain.HandleAsync(request, async _ => { defaultHandlerInvoked = true; });
        
        // Assert - No handler should process the request, default handler should be invoked
        Assert.Empty(results);
        Assert.True(defaultHandlerInvoked);
    }
}

// Conditional handlers for testing
public class ConditionalFirstHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value < 10);
    }

    public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add(nameof(ConditionalFirstHandler));
        return Task.CompletedTask; // Don't call next, terminate the chain
    }
}

public class ConditionalSecondHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 10 && request.Value < 20);
    }

    public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add(nameof(ConditionalSecondHandler));
        return Task.CompletedTask; // Don't call next, terminate the chain
    }
}

public class ConditionalThirdHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 20 && request.Value < 30);
    }

    public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add(nameof(ConditionalThirdHandler));
        return Task.CompletedTask; // Don't call next, terminate the chain
    }
}
