using Forma.Chains.Abstractions;
using Forma.Chains.Configurations;
using Forma.Chains.Extensions;
using Forma.Chains.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Tests.Chains;

public class ChainConfigurationTests
{
    [Fact]
    public async Task ChainBuilderLifetime_ShouldBeConfigurable()
    {
        // Arrange
        var services = new ServiceCollection();

        // Registrazione della catena con ChainBuilder Singleton
        services.AddChain<TestRequest>(options =>
        {
            options.ChainBuilderLifetime = ServiceLifetime.Singleton;
        }, typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();

        // Act - Ottieni due istanze del ChainBuilder
        var builder1 = provider.GetRequiredService<IChainBuilder<TestRequest>>();
        var builder2 = provider.GetRequiredService<IChainBuilder<TestRequest>>();

        // Assert - Le istanze dovrebbero essere le stesse con lifetime Singleton
        Assert.Same(builder1, builder2);
    }

    [Fact]
    public async Task OrderStrategy_Alphabetical_ShouldOrderHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<FirstHandler>();
        services.AddTransient<SecondHandler>();
        services.AddTransient<ThirdHandler>();

        // Registrazione della catena con ordinamento alfabetico
        // Nota: in questo caso, gli handler sono già in ordine alfabetico (First, Second, Third)
        // ma li passiamo in ordine diverso per testare il riordinamento
        services.AddChain<TestRequest>(options =>
        {
            options.OrderStrategy = ChainOrderStrategy.Alphabetical;
        }, typeof(ThirdHandler), typeof(FirstHandler), typeof(SecondHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();

        var request = new TestRequest { Value = 10 };
        var results = new List<string>();
        request.Results = results;

        // Act
        await chain.HandleAsync(request, default);

        // Assert - Gli handler dovrebbero essere eseguiti in ordine alfabetico
        Assert.Equal(3, results.Count);
        Assert.Equal("FirstHandler", results[0]);
        Assert.Equal("SecondHandler", results[1]);
        Assert.Equal("ThirdHandler", results[2]);
    }

    [Fact]
    public async Task HandlerTypeFilter_ShouldFilterHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // services.AddTransient<FirstHandler>();
        // services.AddTransient<SecondHandler>();
        // services.AddTransient<ThirdHandler>();

        // Registrazione della catena con filtro (solo handler che iniziano con "F")
        services.AddChain<TestRequest>(options =>
        {
            options.HandlerTypeFilter = type => type.Name.StartsWith("F");
        }, typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();

        var request = new TestRequest { Value = 10 };
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
        services.AddTransient<ThirdHandler>();

        // Registrazione della catena con validator (esclude SecondHandler)
        services.AddChain<TestRequest>(options =>
        {
            options.HandlerValidator = handler => handler.GetType().Name != "SecondHandler";
        }, typeof(FirstHandler), typeof(SecondHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();

        var request = new TestRequest { Value = 10 };
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
        var services = new ServiceCollection();

        // Registrazione della catena senza handler ma con comportamento ReturnEmpty
        services.AddChain<TestRequest>(options =>
        {
            options.MissingHandlerBehavior = MissingHandlerBehavior.ReturnEmpty;
        });

        var provider = services.BuildServiceProvider();

        // Act & Assert - Non dovrebbe lanciare un'eccezione
        var chain = provider.GetRequiredService<IChainHandler<TestRequest>>();
        Assert.NotNull(chain);
    }

    [Fact]
    public void MissingHandlerBehavior_ThrowException_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Registrazione della catena senza handler
        services.AddChain<TestRequest>(options =>
        {
            options.MissingHandlerBehavior = MissingHandlerBehavior.ThrowException;
        });

        var provider = services.BuildServiceProvider();

        // Act & Assert - Dovrebbe lanciare un'eccezione
        Assert.Throws<InvalidOperationException>(() =>
        {
            provider.GetRequiredService<IChainHandler<TestRequest>>();
        });
    }

    [Fact]
    public async Task ChainWithCustomConfiguration_ShouldWorkWithResponse()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<FirstResponseHandler>();
        services.AddTransient<SecondResponseHandler>();
        services.AddTransient<ThirdResponseHandler>();

        // Registrazione della catena con configurazione personalizzata
        services.AddChain<TestRequest, TestResponse>(options =>
        {
            options.ChainBuilderLifetime = ServiceLifetime.Singleton;
            options.OrderStrategy = ChainOrderStrategy.AsProvided;
        }, typeof(FirstResponseHandler), typeof(SecondResponseHandler), typeof(ThirdResponseHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainHandler<TestRequest, TestResponse>>();

        // Act
        var response = await chain.HandleAsync(new TestRequest { Value = 15 }, default);

        // Assert
        Assert.Equal("SecondHandler: 15", response.Result);
    }
    
}
