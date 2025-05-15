using System.Collections.Concurrent;
using System.Threading.Channels;
using Forma.Core.PubSub.Abstractions;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Forma.PubSub.InMemory.Channels;

/// <summary>
/// Interface for typed event channel
/// </summary>
internal interface IEventChannel
{
    Type EventType { get; }
}

/// <summary>
/// Represents a typed event channel
/// </summary>
internal class EventChannel<TEvent> : IEventChannel where TEvent : IEvent
{
    public Channel<Event<TEvent>> Channel { get; }
    public Type EventType => typeof(TEvent);

    public EventChannel()
    {
        Channel = System.Threading.Channels.Channel.CreateUnbounded<Event<TEvent>>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
    }
}

internal sealed class InMemoryBusImpl : IBus, IHostedService
{    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryBusImpl> _logger;
    private readonly ConcurrentDictionary<Type, IEventChannel> _channels;
    private CancellationTokenSource _stoppingCts;
    private readonly List<Task> _processingTasks;

    public InMemoryBusImpl(
        IServiceProvider serviceProvider,
        ILogger<InMemoryBusImpl> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channels = new ConcurrentDictionary<Type, IEventChannel>();
        _stoppingCts = new CancellationTokenSource();
        _processingTasks = new List<Task>();
    }    /// <summary>
    /// Publish an event to the bus.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        // Get or create a channel for this event type
        var eventChannel = GetOrCreateChannelForType<TEvent>();
        var eventObj = new Event<TEvent>(@event);

        // Write the event to the channel
        return eventChannel.Channel.Writer.WriteAsync(eventObj, cancellationToken).AsTask();
    }

    /// <summary>
    /// Gets or creates a channel for the specified event type
    /// </summary>
    private EventChannel<TEvent> GetOrCreateChannelForType<TEvent>() where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        
        if (_channels.TryGetValue(eventType, out var existingChannel) && existingChannel is EventChannel<TEvent> typedChannel)
        {
            return typedChannel;
        }
        
        var newChannel = new EventChannel<TEvent>();
        
        if (_channels.TryAdd(eventType, newChannel))
        {
            // If we're already running, start processing for the new channel
            if (!_stoppingCts.IsCancellationRequested)
            {
                StartProcessingChannel(newChannel);
            }
        }
        else if (_channels.TryGetValue(eventType, out var race) && race is EventChannel<TEvent> raceTyped)
        {
            // Another thread created the channel before us
            return raceTyped;
        }
        
        return (EventChannel<TEvent>)_channels[eventType];
    }    /// <summary>
    /// Starts processing messages from the specified channel
    /// </summary>
    private void StartProcessingChannel<TEvent>(EventChannel<TEvent> eventChannel) where TEvent : IEvent
    {
        var processingTask = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Starting to process events of type {EventType}", typeof(TEvent).Name);

                while (!_stoppingCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Wait for the next event
                        var @event = await eventChannel.Channel.Reader.ReadAsync(_stoppingCts.Token);
                        
                        // Process the event
                        _ = ProcessEventAsync(@event, _stoppingCts.Token);
                    }
                    catch (OperationCanceledException) when (_stoppingCts.Token.IsCancellationRequested)
                    {
                        // Normal shutdown
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event of type {EventType}", typeof(TEvent).Name);
                        // Continue processing other events
                    }
                }
                
                _logger.LogInformation("Stopped processing events of type {EventType}", typeof(TEvent).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in channel processor for {EventType}", typeof(TEvent).Name);
            }
        }, _stoppingCts.Token);
        
        _processingTasks.Add(processingTask);
    }

    /// <summary>
    /// Processes a single event by finding and invoking the appropriate consumers
    /// </summary>
    private async Task ProcessEventAsync<TEvent>(Event<TEvent> @event, CancellationToken cancellationToken) where TEvent : IEvent
    {
        // Create a new scope for dependency resolution
        using var scope = _serviceProvider.CreateScope();
        
        // Get all handlers for this event type
        var handlers = scope.ServiceProvider.GetServices<IConsume<TEvent>>().ToList();
        
        if (handlers.Count == 0)
        {
            _logger.LogDebug("No handlers defined for event of type {EventType}", typeof(TEvent).Name);
            return;
        }
        
        // Invoke handlers in parallel
        await Parallel.ForEachAsync(
            handlers, 
            cancellationToken,
            async (handler, token) => 
            {
                try
                {
                    await handler.ConsumeAsync(@event.Data!, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in handler {HandlerType} for event {EventType}", 
                        handler.GetType().Name, typeof(TEvent).Name);
                }
            }
        );
    }    /// <summary>
    /// Starts the bus as a hosted service
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = new CancellationTokenSource();

        _logger.LogInformation("Starting InMemoryBus");
        
        // Start processing for all existing channels
        foreach (var channel in _channels.Values)
        {
            StartProcessingChannelInternal(channel);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Internal non-generic method to start processing a channel
    /// </summary>
    private void StartProcessingChannelInternal(IEventChannel channel)
    {
        // Use a type switch to handle different event types without reflection
        switch (channel)
        {
            case EventChannel<IEvent> eventChannel:
                StartProcessingChannel(eventChannel);
                break;
                
            default:                // Use dynamic as a fallback mechanism to call the appropriate generic method
                Type eventType = channel.EventType;
                
                // Verifica che il tipo di evento implementi IEvent
                if (!typeof(IEvent).IsAssignableFrom(eventType))
                {
                    _logger.LogError("Il tipo {eventType} non implementa l'interfaccia IEvent", eventType);
                    break;
                }
                
                dynamic typedChannel = channel;
                try 
                {
                    StartProcessingChannelDynamic(typedChannel);
                }
                catch (RuntimeBinderException ex)
                {
                    _logger.LogError(ex, "Errore durante l'invocazione di StartProcessingChannelDynamic per il tipo {eventType}", eventType);
                }
                break;
        }
    }
    
    /// <summary>
    /// Dynamic helper to invoke the correct StartProcessingChannel method
    /// </summary>
    private void StartProcessingChannelDynamic<TEvent>(EventChannel<TEvent> eventChannel) where TEvent : IEvent
    {
        StartProcessingChannel(eventChannel);
    }

    /// <summary>
    /// Stops the bus and all channel processors
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping InMemoryBus");
        
        // Signal all processing tasks to stop
        if (!_stoppingCts.IsCancellationRequested)
        {
            _stoppingCts.Cancel();
        }
        
        // Wait for all tasks to complete or timeout
        if (_processingTasks.Any())
        {
            var completedTask = await Task.WhenAny(
                Task.WhenAll(_processingTasks),
                Task.Delay(TimeSpan.FromSeconds(5), cancellationToken)
            );
            
            if (completedTask != Task.WhenAll(_processingTasks))
            {
                _logger.LogWarning("Timed out waiting for some channel processors to stop");
            }
        }

        _channels.Clear();
        _processingTasks.Clear();

        _logger.LogInformation("InMemoryBus stopped");
    }
}
