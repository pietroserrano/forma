using Forma.Chains.Abstractions;
using Forma.Chains.Configurations;
using Forma.Chains.Extensions;
using Forma.Chains.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Forma.Tests.Chains;

public class ChainConfigurationTestsTest
{
    [Fact]
    public async Task OrderStrategy_AsProvided_ShouldRespectGivenOrder()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<FirstHandler>();
        services.AddTransient<SecondHandler>();
        services.AddTransient<ThirdHandler>();

        // Pass handlers in a specific order
        services.AddChain<TestRequest>(options =>
        {
            options.OrderStrategy = ChainOrderStrategy.AsProvided;
        }, typeof(SecondHandler), typeof(FirstHandler), typeof(ThirdHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainInvoker<TestRequest>>();

        var request = new TestRequest { Value = 1 };
        var results = new List<string>();
        request.Results = results;

        // Act
        await chain.HandleAsync(request, default);

        // Assert - Handlers should be executed in the provided order
        Assert.Equal(3, results.Count);
        Assert.Equal("SecondHandler", results[0]);
        Assert.Equal("FirstHandler", results[1]);
        Assert.Equal("ThirdHandler", results[2]);
    }

    [Fact]
    public async Task OrderStrategy_ReverseAlphabetical_ShouldOrderHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<FirstHandler>();
        services.AddTransient<SecondHandler>();
        services.AddTransient<ThirdHandler>();

        // Pass handlers in any order, expect reverse alphabetical
        services.AddChain<TestRequest>(options =>
        {
            options.OrderStrategy = ChainOrderStrategy.ReverseAlphabetical;
        }, typeof(FirstHandler), typeof(ThirdHandler), typeof(SecondHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainInvoker<TestRequest>>();

        var request = new TestRequest { Value = 2 };
        var results = new List<string>();
        request.Results = results;

        // Act
        await chain.HandleAsync(request, default);

        // Assert - Handlers should be executed in reverse alphabetical order
        Assert.Equal(3, results.Count);
        Assert.Equal("ThirdHandler", results[0]);
        Assert.Equal("SecondHandler", results[1]);
        Assert.Equal("FirstHandler", results[2]);
    }

    [Fact]
    public async Task OrderStrategy_Alphabetical_ShouldOrderHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<FirstHandler>();
        services.AddTransient<SecondHandler>();
        services.AddTransient<ThirdHandler>();

        // Pass handlers in any order, expect alphabetical
        services.AddChain<TestRequest>(options =>
        {
            options.OrderStrategy = ChainOrderStrategy.Alphabetical;
        }, typeof(ThirdHandler), typeof(SecondHandler), typeof(FirstHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainInvoker<TestRequest>>();

        var request = new TestRequest { Value = 3 };
        var results = new List<string>();
        request.Results = results;

        // Act
        await chain.HandleAsync(request, default);

        // Assert - Handlers should be executed in alphabetical order
        Assert.Equal(3, results.Count);
        Assert.Equal("FirstHandler", results[0]);
        Assert.Equal("SecondHandler", results[1]);
        Assert.Equal("ThirdHandler", results[2]);
    }

    [Fact]
    public async Task OrderStrategy_Default_ShouldOrderHandlersAsProvided()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<FirstHandler>();
        services.AddTransient<SecondHandler>();
        services.AddTransient<ThirdHandler>();

        // Pass handlers in a specific order, expect default (as provided)
        services.AddChain<TestRequest>(options =>
        {
            // No explicit OrderStrategy set, should default to AsProvided
        }, typeof(ThirdHandler), typeof(FirstHandler), typeof(SecondHandler));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainInvoker<TestRequest>>();

        var request = new TestRequest { Value = 4 };
        var results = new List<string>();
        request.Results = results;

        // Act
        await chain.HandleAsync(request, default);

        // Assert - Handlers should be executed in the provided order
        Assert.Equal(3, results.Count);
        Assert.Equal("ThirdHandler", results[0]);
        Assert.Equal("FirstHandler", results[1]);
        Assert.Equal("SecondHandler", results[2]);
    }

    [Fact]
    public async Task OrderStrategy_PriorityAttribute_ShouldOrderByPriority()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddTransient<PriorityHandlerLow>();
        services.AddTransient<PriorityHandlerHigh>();
        services.AddTransient<PriorityHandlerDefault>();

        // Pass handlers in any order, expect order by ChainPriorityAttribute (lower value = higher priority)
        services.AddChain<TestRequest>(options =>
        {
            options.OrderStrategy = ChainOrderStrategy.Priority;
        }, typeof(PriorityHandlerDefault), typeof(PriorityHandlerHigh), typeof(PriorityHandlerLow));

        var provider = services.BuildServiceProvider();
        var chain = provider.GetRequiredService<IChainInvoker<TestRequest>>();

        var request = new TestRequest { Value = 5 };
        var results = new List<string>();
        request.Results = results;

        // Act
        await chain.HandleAsync(request, default);

        // Assert - Handlers should be executed in order of priority: High (0), Default (10), Low (100)
        Assert.Equal(3, results.Count);
        Assert.Equal("PriorityHandlerHigh", results[0]);
        Assert.Equal("PriorityHandlerDefault", results[1]);
        Assert.Equal("PriorityHandlerLow", results[2]);
    }

    // Handler with high priority (0)
    [ChainPriority(0)]
    public class PriorityHandlerHigh : IChainHandler<TestRequest>
    {
        public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            request.Results?.Add(nameof(PriorityHandlerHigh));
            return next(cancellationToken);
        }
    }

    // Handler with default priority (10)
    [ChainPriority(10)]
    public class PriorityHandlerDefault : IChainHandler<TestRequest>
    {
        public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            request.Results?.Add(nameof(PriorityHandlerDefault));
            return next(cancellationToken);
        }
    }

    // Handler with low priority (100)
    [ChainPriority(100)]
    public class PriorityHandlerLow : IChainHandler<TestRequest>
    {
        public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            request.Results?.Add(nameof(PriorityHandlerLow));
            return next(cancellationToken);
        }
    }
}