using Forma.Chains.Abstractions;
using Forma.Chains.Configurations;
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
public class ConfigRequest
{
    public int Value { get; set; }
    public List<string>? Results { get; set; }
}

public class ConfigResponse
{
    public string? Result { get; set; }
}

// Handler per i test di configurazione
public class FirstHandler : IChainHandler<ConfigRequest>
{
    public Task<bool> CanHandleAsync(ConfigRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task HandleAsync(ConfigRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("FirstHandler");
        return next(cancellationToken);
    }
}

public class SecondHandler : IChainHandler<ConfigRequest>
{
    public Task<bool> CanHandleAsync(ConfigRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
    
    public Task HandleAsync(ConfigRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("SecondHandler");
        return next(cancellationToken);
    }
}

public class ThirdHandler : IChainHandler<ConfigRequest>
{
    public Task<bool> CanHandleAsync(ConfigRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task HandleAsync(ConfigRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("ThirdHandler");
        return next(cancellationToken);
    }
}

// Handler per il test con response
public class FirstResponseHandler : IChainHandler<ConfigRequest, ConfigResponse>
{
    public Task<bool> CanHandleAsync(ConfigRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value < 10);
    }

    public Task<ConfigResponse> HandleAsync(ConfigRequest request, Func<CancellationToken, Task<ConfigResponse>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ConfigResponse { Result = $"FirstHandler: {request.Value}" });
    }
}

public class SecondResponseHandler : IChainHandler<ConfigRequest, ConfigResponse>
{
    public Task<bool> CanHandleAsync(ConfigRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 10 && request.Value < 20);
    }

    public Task<ConfigResponse> HandleAsync(ConfigRequest request, Func<CancellationToken, Task<ConfigResponse>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ConfigResponse { Result = $"SecondHandler: {request.Value}" });
    }
}

public class ThirdResponseHandler : IChainHandler<ConfigRequest, ConfigResponse>
{
    public Task<bool> CanHandleAsync(ConfigRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Value >= 20);
    }

    public Task<ConfigResponse> HandleAsync(ConfigRequest request, Func<CancellationToken, Task<ConfigResponse>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ConfigResponse { Result = $"ThirdHandler: {request.Value}" });
    }
}

public class ChainConfigurationTests
{
    [Fact]
    public void ChainBuilderLifetime_ShouldBeConfigurable()
    {
        // Arrange
        var services = new ServiceCollection();        // Registrazione della catena con ChainBuilder Singleton
        services.AddChain<ConfigRequest>(options =>
        {
            options.ChainBuilderLifetime = ServiceLifetime.Singleton;
        }, typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();

        // Act - Ottieni due istanze del ChainBuilder
        var builder1 = provider.GetRequiredService<IChainBuilder<ConfigRequest>>();
        var builder2 = provider.GetRequiredService<IChainBuilder<ConfigRequest>>();

        // Assert - Le istanze dovrebbero essere le stesse con lifetime Singleton
        Assert.Same(builder1, builder2);
    }

    [Fact]
    public void ChainBuilderLifetime_Scoped_ShouldCreateDifferentInstancesInDifferentScopes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Registrazione della catena con ChainBuilder Scoped
        services.AddChain<TestRequest>(options =>
        {
            options.ChainBuilderLifetime = ServiceLifetime.Scoped;
        }, typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();

        // Act - Ottieni istanze da diversi scope
        IChainBuilder<TestRequest> builder1;
        IChainBuilder<TestRequest> builder2;
        IChainBuilder<TestRequest> builder3;

        using (var scope1 = provider.CreateScope())
        {
            builder1 = scope1.ServiceProvider.GetRequiredService<IChainBuilder<TestRequest>>();
            builder2 = scope1.ServiceProvider.GetRequiredService<IChainBuilder<TestRequest>>();
        }

        using (var scope2 = provider.CreateScope())
        {
            builder3 = scope2.ServiceProvider.GetRequiredService<IChainBuilder<TestRequest>>();
        }

        // Assert - Le istanze dovrebbero essere le stesse all'interno dello stesso scope ma diverse tra scope diversi
        Assert.Same(builder1, builder2);
        Assert.NotSame(builder1, builder3);
    }

    [Fact]
    public void ChainBuilderLifetime_Transient_ShouldCreateDifferentInstances()
    {
        // Arrange
        var services = new ServiceCollection();

        // Registrazione della catena con ChainBuilder Transient
        services.AddChain<TestRequest>(options =>
        {
            options.ChainBuilderLifetime = ServiceLifetime.Transient;
        }, typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();

        // Act - Ottieni due istanze del ChainBuilder
        var builder1 = provider.GetRequiredService<IChainBuilder<TestRequest>>();
        var builder2 = provider.GetRequiredService<IChainBuilder<TestRequest>>();

        // Assert - Le istanze dovrebbero essere diverse con lifetime Transient
        Assert.NotSame(builder1, builder2);
    }

    [Fact]
    public async Task HandlerTypeFilter_ShouldFilterHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddChain<ConfigRequest>(options =>
        {
            options.HandlerTypeFilter = type => type.Name.StartsWith("F");
        }, typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainInvoker<ConfigRequest>>();

        var request = new ConfigRequest { Value = 10 };
        var results = new List<string>();
        request.Results = results;

        // Act
        await chain.HandleAsync(request, default);

        // Assert - Solo FirstHandler dovrebbe essere eseguito
        Assert.Single(results);
        Assert.Equal("FirstHandler", results[0]);
    }

    [Fact]
    public async Task HandlerValidator_ShouldValidateHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<FirstHandler>();
        services.AddTransient<SecondHandler>();
        services.AddTransient<ThirdHandler>();        // Registrazione della catena con validator (esclude SecondHandler)
        services.AddChain<ConfigRequest>(options =>
        {
            options.HandlerValidator = handler => handler.GetType().Name != "SecondHandler";
        }, typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainInvoker<ConfigRequest>>();

        var request = new ConfigRequest { Value = 10 };
        var results = new List<string>();
        request.Results = results;

        // Act
        await chain.HandleAsync(request, default);

        // Assert - SecondHandler dovrebbe essere escluso
        Assert.Equal(2, results.Count);
        Assert.Equal("FirstHandler", results[0]);
        Assert.Equal("ThirdHandler", results[1]);
    }

    [Fact]
    public void MissingHandlerBehavior_ReturnEmpty_ShouldNotThrowException()
    {
        // Arrange
        var services = new ServiceCollection();        // Registrazione della catena senza handler ma con comportamento ReturnEmpty
        services.AddChain<ConfigRequest>(options =>
        {
            options.MissingHandlerBehavior = MissingHandlerBehavior.ReturnEmpty;
        });

        var provider = services.BuildServiceProvider();

        // Act & Assert - Non dovrebbe lanciare un'eccezione
        var chain = provider.GetRequiredService<IChainInvoker<ConfigRequest>>();
        Assert.NotNull(chain);
    }

    [Fact]
    public void MissingHandlerBehavior_ThrowException_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();        // Registrazione della catena senza handler
        services.AddChain<ConfigRequest>(options =>
        {
            options.MissingHandlerBehavior = MissingHandlerBehavior.ThrowException;
        });

        var provider = services.BuildServiceProvider();

        // Act & Assert - Dovrebbe lanciare un'eccezione
        Assert.Throws<InvalidOperationException>(() =>
        {
            provider.GetRequiredService<IChainInvoker<ConfigRequest>>();
        });
    }

    [Fact]
    public async Task ChainWithCustomConfiguration_ShouldWorkWithResponse()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<FirstResponseHandler>();
        services.AddTransient<SecondResponseHandler>();
        services.AddTransient<ThirdResponseHandler>();        // Registrazione della catena con configurazione personalizzata
        services.AddChain<ConfigRequest, ConfigResponse>(options =>
        {
            options.ChainBuilderLifetime = ServiceLifetime.Singleton;
            options.OrderStrategy = ChainOrderStrategy.AsProvided;
        }, typeof(FirstResponseHandler), typeof(SecondResponseHandler), typeof(ThirdResponseHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainInvoker<ConfigRequest, ConfigResponse>>();

        // Act
        var response = await chain.HandleAsync(new ConfigRequest { Value = 15 }, default);

        // Assert
        Assert.Equal("SecondHandler: 15", response.Result);
    }

    [Fact]
    public async Task MultipleConfigurations_ShouldBeIndependent()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddChain<ConfigRequest>(options =>
        {
            options.HandlerValidator = handler => handler.GetType().Name != "SecondHandler";
        }, typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        services.AddChain<ConfigRequest, ConfigResponse>(options =>
        {
            options.HandlerValidator = handler => handler.GetType().Name != "FirstResponseHandler";
        }, typeof(FirstResponseHandler), typeof(SecondResponseHandler), typeof(ThirdResponseHandler));

        var provider = services.BuildServiceProvider();
        
        // Chain without response
        var chain1 = provider.GetRequiredService<IChainInvoker<ConfigRequest>>();
        var request1 = new ConfigRequest { Value = 10 };
        var results = new List<string>();
        request1.Results = results;

        // Chain with response
        var chain2 = provider.GetRequiredService<IChainInvoker<ConfigRequest, ConfigResponse>>();
        var request2 = new ConfigRequest { Value = 15 };

        // Act
        await chain1.HandleAsync(request1, default);
        var response = await chain2.HandleAsync(request2);

        // Assert - Per la prima catena, SecondHandler dovrebbe essere escluso
        Assert.Equal(2, results.Count);
        Assert.Equal("FirstHandler", results[0]);
        Assert.Equal("ThirdHandler", results[1]);

        // Assert - Per la seconda catena, FirstHandler dovrebbe essere escluso quindi SecondHandler gestirà la richiesta
        Assert.Equal("SecondHandler: 15", response.Result);
    }
}
