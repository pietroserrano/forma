using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Forma.Tests.Mediator;

public class MediatorIntegrationTests
{
    [Fact]
    public async Task Mediator_WithPipeline_ExecutesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionTracker = new ExecutionTracker();

        services.AddSingleton(executionTracker);
        services.AddRequestMediator(c =>
        {
            c.AddRequestPreProcessor<TrackingPreProcessor>();
            c.AddRequestPostProcessor<TrackingPostProcessor>();
            c.RegisterServicesFromAssemblies(typeof(MediatorIntegrationTests).Assembly, typeof(MediatorIntegrationTests).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IRequestMediator>();

        // Act
        var response = await mediator.SendAsync(new TrackingRequest());

        // Assert
        Assert.Equal(3, executionTracker.Steps.Count);
        Assert.Equal("PreProcessor", executionTracker.Steps[0]);
        Assert.Equal("Handler", executionTracker.Steps[1]);
        Assert.Equal("PostProcessor", executionTracker.Steps[2]);
        Assert.Equal("Completed", response.Result);
    }

    [Fact]
    public async Task Mediator_PreProcessorThrowsException_ExceptionIsPropagate()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionTracker = new ExecutionTracker();

        services.AddSingleton(executionTracker);
        services.AddRequestMediator(c =>
        {
            c.AddRequestPreProcessor<FailingPreProcessor>();
            c.RegisterServicesFromAssemblies(typeof(MediatorIntegrationTests).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IRequestMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await mediator.SendAsync(new TrackingRequest()));

        Assert.Equal("PreProcessor failed", exception.Message);
        Assert.Equal(1, executionTracker.Steps.Count);
        Assert.Equal("FailingPreProcessor", executionTracker.Steps[0]);
    }

    [Fact]
    public async Task Mediator_HandlerThrowsException_ExceptionIsPropagated()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionTracker = new ExecutionTracker();

        services.AddSingleton(executionTracker);
        services.AddRequestMediator(c =>
        {
            c.RegisterServicesFromAssemblies(typeof(MediatorIntegrationTests).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IRequestMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await mediator.SendAsync(new FailingRequest()));

        Assert.Equal("Handler failed", exception.Message);
        Assert.Equal(1, executionTracker.Steps.Count);
        Assert.Equal("FailingHandler", executionTracker.Steps[0]);
    }

    [Fact]
    public async Task Mediator_PostProcessorThrowsException_ExceptionIsPropagated()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionTracker = new ExecutionTracker();

        services.AddSingleton(executionTracker);
        services.AddRequestMediator(c =>
        {
            c.AddRequestPostProcessor<FailingPostProcessor>();
            c.RegisterServicesFromAssemblies(typeof(MediatorIntegrationTests).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IRequestMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await mediator.SendAsync(new TrackingRequest()));

        Assert.Equal("PostProcessor failed", exception.Message);
        Assert.Equal(2, executionTracker.Steps.Count);
        Assert.Equal("Handler", executionTracker.Steps[0]);
        Assert.Equal("FailingPostProcessor", executionTracker.Steps[1]);
    }

    [Fact]
    public async Task Mediator_NoHandlerRegistered_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRequestMediator(c =>
        {
            // Non registriamo nessun handler per UnregisteredRequest
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IRequestMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await mediator.SendAsync(new UnregisteredRequest()));
    }
}

// Classi di supporto
public class ExecutionTracker
{
    public List<string> Steps { get; } = new();

    public void Track(string step)
    {
        Steps.Add(step);
    }
}

public class TrackingRequest : IRequest<TrackingResponse> { }
public class TrackingResponse
{
    public string? Result { get; set; }
}

public class TrackingRequestHandler : IHandler<TrackingRequest, TrackingResponse>
{
    private readonly ExecutionTracker _tracker;

    public TrackingRequestHandler(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public Task<TrackingResponse> HandleAsync(TrackingRequest request, CancellationToken cancellationToken)
    {
        _tracker.Track("Handler");
        return Task.FromResult(new TrackingResponse { Result = "Completed" });
    }
}

public class TrackingPreProcessor : IRequestPreProcessor<TrackingRequest>
{
    private readonly ExecutionTracker _tracker;

    public TrackingPreProcessor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public Task ProcessAsync(TrackingRequest message, CancellationToken cancellationToken)
    {
        _tracker.Track("PreProcessor");
        return Task.CompletedTask;
    }
}

public class TrackingPostProcessor : IRequestPostProcessor<TrackingRequest, TrackingResponse>
{
    private readonly ExecutionTracker _tracker;

    public TrackingPostProcessor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public Task ProcessAsync(TrackingRequest request, TrackingResponse response, CancellationToken cancellationToken)
    {
        _tracker.Track("PostProcessor");
        return Task.CompletedTask;
    }
}

// Nuove classi per test di fallimento
public class FailingPreProcessor : IRequestPreProcessor<TrackingRequest>
{
    private readonly ExecutionTracker _tracker;

    public FailingPreProcessor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public Task ProcessAsync(TrackingRequest message, CancellationToken cancellationToken)
    {
        _tracker.Track("FailingPreProcessor");
        throw new InvalidOperationException("PreProcessor failed");
    }
}

public class FailingRequest : IRequest<FailingResponse> { }
public class FailingResponse
{
    public string? Result { get; set; }
}

public class FailingRequestHandler : IHandler<FailingRequest, FailingResponse>
{
    private readonly ExecutionTracker _tracker;

    public FailingRequestHandler(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public Task<FailingResponse> HandleAsync(FailingRequest request, CancellationToken cancellationToken)
    {
        _tracker.Track("FailingHandler");
        throw new InvalidOperationException("Handler failed");
    }
}

public class FailingPostProcessor : IRequestPostProcessor<TrackingRequest, TrackingResponse>
{
    private readonly ExecutionTracker _tracker;

    public FailingPostProcessor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public Task ProcessAsync(TrackingRequest request, TrackingResponse response, CancellationToken cancellationToken)
    {
        _tracker.Track("FailingPostProcessor");
        throw new InvalidOperationException("PostProcessor failed");
    }
}

public class UnregisteredRequest : IRequest<UnregisteredResponse> { }
public class UnregisteredResponse
{
    public string? Result { get; set; }
}
