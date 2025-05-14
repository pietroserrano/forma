using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Tests.Mediator;

public class BehaviorTests
{
    [Fact]
    public async Task RequestPreProcessorBehavior_ExecutesAllPreProcessors()
    {
        // Arrange
        var executed = new List<string>();
        var preProcessors = new List<IRequestPreProcessor<BehaviorRequest>>
        {
            new TestPreProcessor(executed, "Pre1"),
            new TestPreProcessor(executed, "Pre2")
        };

        var behavior = new RequestPreProcessorBehavior<BehaviorRequest, BehaviorResponse>(preProcessors);

        // Act
        var response = await behavior.HandleAsync(
            new BehaviorRequest(),
            CancellationToken.None,
            _ => Task.FromResult(new BehaviorResponse()));

        // Assert
        Assert.Equal(2, executed.Count);
        Assert.Equal("Pre1", executed[0]);
        Assert.Equal("Pre2", executed[1]);
    }

    [Fact]
    public async Task RequestPostProcessorBehavior_ExecutesAllPostProcessors()
    {
        // Arrange
        var executed = new List<string>();
        var postProcessors = new List<IRequestPostProcessor<BehaviorRequest, BehaviorResponse>>
        {
            new TestPostProcessor(executed, "Post1"),
            new TestPostProcessor(executed, "Post2")
        };

        var behavior = new RequestPostProcessorBehavior<BehaviorRequest, BehaviorResponse>(postProcessors);

        // Act
        var response = await behavior.HandleAsync(
            new BehaviorRequest(),
            CancellationToken.None,
            _ => Task.FromResult(new BehaviorResponse()));

        // Assert
        Assert.Equal(2, executed.Count);
        Assert.Equal("Post1", executed[0]);
        Assert.Equal("Post2", executed[1]);
    }

    [Fact]
    public async Task Behavior_Pipeline_ExecutesInCorrectOrder()
    {
        // Arrange
        var executed = new List<string>();

        var preProcessor = new TestPreProcessor(executed, "Pre");
        var postProcessor = new TestPostProcessor(executed, "Post");

        var preBehavior = new RequestPreProcessorBehavior<BehaviorRequest, BehaviorResponse>(new[] { preProcessor });
        var postBehavior = new RequestPostProcessorBehavior<BehaviorRequest, BehaviorResponse>(new[] { postProcessor });

        // Act
        var response = await preBehavior.HandleAsync(
            new BehaviorRequest(),
            CancellationToken.None,
            _ => postBehavior.HandleAsync(
                new BehaviorRequest(),
                CancellationToken.None,
                __ =>
                {
                    executed.Add("Handler");
                    return Task.FromResult(new BehaviorResponse());
                }));

        // Assert
        Assert.Equal(3, executed.Count);
        Assert.Equal("Pre", executed[0]);
        Assert.Equal("Handler", executed[1]);
        Assert.Equal("Post", executed[2]);
    }

    [Fact]
    public async Task RequestPreProcessorBehavior_WithDependencyInjection_ExecutesAllPreProcessors()
    {
        // Arrange
        var executed = new List<string>();
        var services = new ServiceCollection();

        services.AddSingleton(executed);
        services.AddSingleton<IRequestPreProcessor<BehaviorRequest>>(sp => new TestPreProcessor(sp.GetRequiredService<List<string>>(), "Pre1"));
        services.AddSingleton<IRequestPreProcessor<BehaviorRequest>>(sp => new TestPreProcessor(sp.GetRequiredService<List<string>>(), "Pre2"));

        var serviceProvider = services.BuildServiceProvider();
        var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<BehaviorRequest>>().ToList();

        var behavior = new RequestPreProcessorBehavior<BehaviorRequest, BehaviorResponse>(preProcessors);

        // Act
        var response = await behavior.HandleAsync(
            new BehaviorRequest(),
            CancellationToken.None,
            _ => Task.FromResult(new BehaviorResponse()));

        // Assert
        Assert.Equal(2, executed.Count);
        Assert.Equal("Pre1", executed[0]);
        Assert.Equal("Pre2", executed[1]);
    }

    [Fact]
    public async Task RequestPostProcessorBehavior_WithDependencyInjection_ExecutesAllPostProcessors()
    {
        // Arrange
        var executed = new List<string>();
        var services = new ServiceCollection();

        services.AddSingleton(executed);
        services.AddSingleton<IRequestPostProcessor<BehaviorRequest, BehaviorResponse>>(
            sp => new TestPostProcessor(sp.GetRequiredService<List<string>>(), "Post1"));
        services.AddSingleton<IRequestPostProcessor<BehaviorRequest, BehaviorResponse>>(
            sp => new TestPostProcessor(sp.GetRequiredService<List<string>>(), "Post2"));

        var serviceProvider = services.BuildServiceProvider();
        var postProcessors = serviceProvider.GetServices<IRequestPostProcessor<BehaviorRequest, BehaviorResponse>>().ToList();

        var behavior = new RequestPostProcessorBehavior<BehaviorRequest, BehaviorResponse>(postProcessors);

        // Act
        var response = await behavior.HandleAsync(
            new BehaviorRequest(),
            CancellationToken.None,
            _ => Task.FromResult(new BehaviorResponse()));

        // Assert
        Assert.Equal(2, executed.Count);
        Assert.Equal("Post1", executed[0]);
        Assert.Equal("Post2", executed[1]);
    }

    [Fact]
    public async Task CompleteProcessorPipeline_WithDependencyInjection_ExecutesInCorrectOrder()
    {
        // Arrange
        var executed = new List<string>();
        var services = new ServiceCollection();

        services.AddSingleton(executed);
        services.AddSingleton<IRequestPreProcessor<BehaviorRequest>>(
            sp => new TestPreProcessor(sp.GetRequiredService<List<string>>(), "Pre1"));
        services.AddSingleton<IRequestPreProcessor<BehaviorRequest>>(
            sp => new TestPreProcessor(sp.GetRequiredService<List<string>>(), "Pre2"));
        services.AddSingleton<IRequestPostProcessor<BehaviorRequest, BehaviorResponse>>(
            sp => new TestPostProcessor(sp.GetRequiredService<List<string>>(), "Post1"));
        services.AddSingleton<IRequestPostProcessor<BehaviorRequest, BehaviorResponse>>(
            sp => new TestPostProcessor(sp.GetRequiredService<List<string>>(), "Post2"));

        var serviceProvider = services.BuildServiceProvider();
        var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<BehaviorRequest>>().ToList();
        var postProcessors = serviceProvider.GetServices<IRequestPostProcessor<BehaviorRequest, BehaviorResponse>>().ToList();

        var preBehavior = new RequestPreProcessorBehavior<BehaviorRequest, BehaviorResponse>(preProcessors);
        var postBehavior = new RequestPostProcessorBehavior<BehaviorRequest, BehaviorResponse>(postProcessors);

        // Act
        var response = await preBehavior.HandleAsync(
            new BehaviorRequest(),
            CancellationToken.None,
            _ => postBehavior.HandleAsync(
                new BehaviorRequest(),
                CancellationToken.None,
                __ =>
                {
                    executed.Add("Handler");
                    return Task.FromResult(new BehaviorResponse());
                }));

        // Assert
        Assert.Equal(5, executed.Count);
        Assert.Equal("Pre1", executed[0]);
        Assert.Equal("Pre2", executed[1]);
        Assert.Equal("Handler", executed[2]);
        Assert.Equal("Post1", executed[3]);
        Assert.Equal("Post2", executed[4]);
    }

    [Fact]
    public async Task CanRegisterMultiplePreProcessorsOfDifferentTypes()
    {
        // Arrange
        var executed = new List<string>();
        var services = new ServiceCollection();

        services.AddSingleton(executed);
        services.AddSingleton<IRequestPreProcessor<BehaviorRequest>>(
            sp => new TestPreProcessor(sp.GetRequiredService<List<string>>(), "Regular"));
        services.AddSingleton<IRequestPreProcessor<BehaviorRequest>>(
            sp => new LoggingPreProcessor(sp.GetRequiredService<List<string>>()));

        var serviceProvider = services.BuildServiceProvider();
        var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<BehaviorRequest>>().ToList();

        var behavior = new RequestPreProcessorBehavior<BehaviorRequest, BehaviorResponse>(preProcessors);

        // Act
        var response = await behavior.HandleAsync(
            new BehaviorRequest(),
            CancellationToken.None,
            _ => Task.FromResult(new BehaviorResponse()));

        // Assert
        Assert.Equal(2, executed.Count);
        Assert.Contains("Regular", executed);
        Assert.Contains("Logging", executed);
    }
}

// Classi di supporto
public class BehaviorRequest : IRequest<BehaviorResponse> { }
public class BehaviorResponse { }

public class TestPreProcessor : IRequestPreProcessor<BehaviorRequest>
{
    private readonly List<string> _executed;
    private readonly string _name;

    public TestPreProcessor(List<string> executed, string name)
    {
        _executed = executed;
        _name = name;
    }

    public Task ProcessAsync(BehaviorRequest message, CancellationToken cancellationToken)
    {
        _executed.Add(_name);
        return Task.CompletedTask;
    }
}

public class TestPostProcessor : IRequestPostProcessor<BehaviorRequest, BehaviorResponse>
{
    private readonly List<string> _executed;
    private readonly string _name;

    public TestPostProcessor(List<string> executed, string name)
    {
        _executed = executed;
        _name = name;
    }

    public Task ProcessAsync(BehaviorRequest request, BehaviorResponse response, CancellationToken cancellationToken)
    {
        _executed.Add(_name);
        return Task.CompletedTask;
    }
}

public class LoggingPreProcessor : IRequestPreProcessor<BehaviorRequest>
{
    private readonly List<string> _executed;

    public LoggingPreProcessor(List<string> executed)
    {
        _executed = executed;
    }

    public Task ProcessAsync(BehaviorRequest message, CancellationToken cancellationToken)
    {
        _executed.Add("Logging");
        return Task.CompletedTask;
    }
}
