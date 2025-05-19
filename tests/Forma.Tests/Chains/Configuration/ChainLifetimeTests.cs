using Forma.Chains.Abstractions;
using Forma.Chains.Extensions;
using Forma.Chains.Implementations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forma.Tests.Chains.Configuration;

// Classi di supporto per i test
public class LifetimeRequest
{
    public int Value { get; set; }
    public List<string>? Results { get; set; }
}

public class LifetimeResponse
{
    public string? Result { get; set; }
}

// Handler per i test
public class LifetimeFirstHandler : IChainHandler<LifetimeRequest>
{
    public Task<bool> CanHandleAsync(LifetimeRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task HandleAsync(LifetimeRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("FirstHandler");
        return next(cancellationToken);
    }
}

public class LifetimeSecondHandler : IChainHandler<LifetimeRequest>
{
    public Task<bool> CanHandleAsync(LifetimeRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
    
    public Task HandleAsync(LifetimeRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("SecondHandler");
        return next(cancellationToken);
    }
}

public class LifetimeThirdHandler : IChainHandler<LifetimeRequest>
{
    public Task<bool> CanHandleAsync(LifetimeRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task HandleAsync(LifetimeRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("ThirdHandler");
        return next(cancellationToken);
    }
}

// Handler con response
public class LifetimeFirstResponseHandler : IChainHandler<LifetimeRequest, LifetimeResponse>
{
    public Task<bool> CanHandleAsync(LifetimeRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value < 10);
    }

    public Task<LifetimeResponse> HandleAsync(LifetimeRequest request, Func<CancellationToken, Task<LifetimeResponse>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LifetimeResponse { Result = $"FirstHandler: {request.Value}" });
    }
}

public class ChainLifetimeTests
{    [Fact]
    public void DefaultLifetime_ShouldNotBeTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Registrazione della catena con configurazione di default
        services.AddChain<LifetimeRequest>(typeof(LifetimeFirstHandler));
        
        var provider = services.BuildServiceProvider();
        
        // Act - Get two instances
        var instance1 = provider.GetRequiredService<IChainInvoker<LifetimeRequest>>();
        var instance2 = provider.GetRequiredService<IChainInvoker<LifetimeRequest>>();
        
        // Assert - Should be different instances (transient)
        Assert.Same(instance1, instance2);
    }
    
    [Fact]
    public void Singleton_ShouldHaveSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Registrazione della catena con lifetime Singleton
        services.AddChain<LifetimeRequest>(options => {
            options.ChainHandlerLifetime = ServiceLifetime.Singleton;
        }, typeof(LifetimeFirstHandler));
        
        var provider = services.BuildServiceProvider();
        
        // Act - Get two instances
        var instance1 = provider.GetRequiredService<IChainInvoker<LifetimeRequest>>();
        var instance2 = provider.GetRequiredService<IChainInvoker<LifetimeRequest>>();
        
        // Assert - Should be the same instance (singleton)
        Assert.Same(instance1, instance2);
    }
    
    [Fact]
    public void Scoped_ShouldHaveSameInstanceInSameScope()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Registrazione della catena con lifetime Scoped
        services.AddChain<LifetimeRequest>(options =>
        {
            options.ChainHandlerLifetime = ServiceLifetime.Scoped;
            options.ChainBuilderLifetime = ServiceLifetime.Scoped;
        }, typeof(LifetimeFirstHandler));
        
        var provider = services.BuildServiceProvider();
        
        // Act - Get instances in same and different scopes
        IChainInvoker<LifetimeRequest> instance1;
        IChainInvoker<LifetimeRequest> instance2;
        IChainInvoker<LifetimeRequest> instance3;
        
        using (var scope1 = provider.CreateScope())
        {
            instance1 = scope1.ServiceProvider.GetRequiredService<IChainInvoker<LifetimeRequest>>();
            instance2 = scope1.ServiceProvider.GetRequiredService<IChainInvoker<LifetimeRequest>>();
        }
        
        using (var scope2 = provider.CreateScope())
        {
            instance3 = scope2.ServiceProvider.GetRequiredService<IChainInvoker<LifetimeRequest>>();
        }
        
        // Assert - Same scope should have same instance, different scopes should have different instances
        Assert.Same(instance1, instance2);
        Assert.NotSame(instance1, instance3);
    }
    
    [Fact]
    public void ChainWithResponse_SingletonLifetime_ShouldHaveSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Registrazione della catena con lifetime Singleton
        services.AddChain<LifetimeRequest, LifetimeResponse>(options => {
            options.ChainHandlerLifetime = ServiceLifetime.Singleton;
        }, typeof(LifetimeFirstResponseHandler));
        
        var provider = services.BuildServiceProvider();
        
        // Act - Get two instances
        var instance1 = provider.GetRequiredService<IChainInvoker<LifetimeRequest, LifetimeResponse>>();
        var instance2 = provider.GetRequiredService<IChainInvoker<LifetimeRequest, LifetimeResponse>>();
        
        // Assert - Should be the same instance (singleton)
        Assert.Same(instance1, instance2);
    }
    
    [Fact]
    public void MixedLifetimes_ShouldResolveCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Registrazione di due catene con diverse configurazioni di lifetime
        services.AddChain<LifetimeRequest>(options =>
        {
            options.ChainHandlerLifetime = ServiceLifetime.Singleton;
            options.ChainBuilderLifetime = ServiceLifetime.Singleton;
        }, typeof(LifetimeFirstHandler));
        
        services.AddChain<LifetimeRequest, LifetimeResponse>(options =>
        {
            options.ChainHandlerLifetime = ServiceLifetime.Transient;
            options.ChainBuilderLifetime = ServiceLifetime.Transient;
        }, typeof(LifetimeFirstResponseHandler));
        
        var provider = services.BuildServiceProvider();
        
        // Act
        var instance1 = provider.GetRequiredService<IChainInvoker<LifetimeRequest>>();
        var instance2 = provider.GetRequiredService<IChainInvoker<LifetimeRequest>>();
        
        var instance3 = provider.GetRequiredService<IChainInvoker<LifetimeRequest, LifetimeResponse>>();
        var instance4 = provider.GetRequiredService<IChainInvoker<LifetimeRequest, LifetimeResponse>>();
        
        // Assert
        Assert.Same(instance1, instance2); // Singleton - same instance
        Assert.NotSame(instance3, instance4); // Transient - different instances
    }
    
    [Fact]
    public async Task DifferentLifetimes_ShouldNotAffectExecution()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Registrazione di due catene per lo stesso tipo di richiesta ma con diverse configurazioni di lifetime
        services.AddChain<LifetimeRequest>(options => {
            options.ChainHandlerLifetime = ServiceLifetime.Singleton;
            options.Name = "singleton-chain";
        }, typeof(LifetimeFirstHandler), typeof(LifetimeSecondHandler), typeof(LifetimeThirdHandler));
        
        services.AddChain<LifetimeRequest>(options => {
            options.ChainHandlerLifetime = ServiceLifetime.Transient;
            options.Name = "transient-chain";
        }, typeof(LifetimeFirstHandler), typeof(LifetimeSecondHandler), typeof(LifetimeThirdHandler));
        
        var provider = services.BuildServiceProvider();
        
        // Get chains
        var singletonChain = provider.GetRequiredKeyedService<IChainInvoker<LifetimeRequest>>("singleton-chain");
        var transientChain = provider.GetRequiredKeyedService<IChainInvoker<LifetimeRequest>>("transient-chain");
        
        // Act - Execute both chains
        var request1 = new LifetimeRequest { Value = 10 };
        request1.Results = new List<string>();
        
        var request2 = new LifetimeRequest { Value = 20 };
        request2.Results = new List<string>();
        
        await singletonChain.HandleAsync(request1, default);
        await transientChain.HandleAsync(request2, default);
        
        // Assert - Both executions should produce the same results regardless of lifetime
        Assert.Equal(3, request1.Results.Count);
        Assert.Equal(3, request2.Results.Count);
        
        Assert.Equal("FirstHandler", request1.Results[0]);
        Assert.Equal("SecondHandler", request1.Results[1]);
        Assert.Equal("ThirdHandler", request1.Results[2]);
        
        Assert.Equal("FirstHandler", request2.Results[0]);
        Assert.Equal("SecondHandler", request2.Results[1]);
        Assert.Equal("ThirdHandler", request2.Results[2]);
    }
}
