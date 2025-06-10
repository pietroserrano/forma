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
    /// Publishes a batch of events efficiently
    /// </summary>
    /// <typeparam name="TEvent">The type of events</typeparam>
    /// <param name="events">Collection of events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the completion of the batch publish</returns>
    public async Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        if (events == null) throw new ArgumentNullException(nameof(events));
        
        // Get or create a channel for this event type
        var eventChannel = GetOrCreateChannelForType<TEvent>();
        var writer = eventChannel.Channel.Writer;
        
        // Process all events as a batch
        foreach (var @event in events)
        {
            var eventObj = new Event<TEvent>(@event);
            await writer.WriteAsync(eventObj, cancellationToken);
        }
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
                
                // Keep track of pending tasks to ensure proper cleanup
                var pendingTasks = new ConcurrentBag<Task>();
                var batchSize = 0;

                while (!_stoppingCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Try to process events in batches for better performance
                        const int maxBatchSize = 100; // Process up to 100 events at once
                        var batch = new List<Event<TEvent>>(maxBatchSize);
                        
                        // First, get one event (waiting if necessary)
                        if (await eventChannel.Channel.Reader.WaitToReadAsync(_stoppingCts.Token))
                        {
                            if (eventChannel.Channel.Reader.TryRead(out var firstEvent))
                            {
                                batch.Add(firstEvent);
                                
                                // Try to read more events without waiting (up to batch size)
                                while (batch.Count < maxBatchSize && eventChannel.Channel.Reader.TryRead(out var nextEvent))
                                {
                                    batch.Add(nextEvent);
                                }
                            }
                        }
                        
                        // Process the batch of events
                        if (batch.Count > 0)
                        {
                            // Create tasks for processing each event
                            foreach (var @event in batch)
                            {
                                var processTask = ProcessEventAsync(@event, _stoppingCts.Token);
                                pendingTasks.Add(processTask);
                            }
                            
                            // Keep track of how many pending tasks we have
                            batchSize += batch.Count;
                            
                            // If we have accumulated enough tasks, wait for some to complete
                            const int cleanupThreshold = 1000;
                            if (batchSize >= cleanupThreshold)
                            {
                                // Wait for some tasks to complete without blocking processing
                                var cleanupTask = Task.Run(async () => {
                                    // Get all pending tasks
                                    var tasks = pendingTasks.ToArray();
                                    
                                    // Wait for any task to complete
                                    if (tasks.Length > 0)
                                    {
                                        await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(10));
                                    }
                                    
                                    // Clean up completed tasks
                                    var completedCount = tasks.Count(t => t.IsCompleted);
                                    if (completedCount > 0)
                                    {
                                        var newPendingTasks = new ConcurrentBag<Task>();
                                        foreach (var task in pendingTasks)
                                        {
                                            if (!task.IsCompleted)
                                            {
                                                newPendingTasks.Add(task);
                                            }
                                        }
                                        
                                        pendingTasks = newPendingTasks;
                                        batchSize -= completedCount;
                                    }
                                });
                            }
                        }
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
                
                // When shutting down, wait briefly for pending tasks to complete
                if (!pendingTasks.IsEmpty)
                {
                    _logger.LogDebug("Waiting for {Count} pending tasks to complete for {EventType}",
                        pendingTasks.Count, typeof(TEvent).Name);
                    
                    try 
                    {
                        // Use a timeout to avoid hanging indefinitely
                        await Task.WhenAny(
                            Task.WhenAll(pendingTasks), 
                            Task.Delay(TimeSpan.FromSeconds(2))
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        // We're shutting down, so we expect cancelation
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
    }/// <summary>
    /// Processes a single event by finding and invoking the appropriate consumers
    /// </summary>
    private async Task ProcessEventAsync<TEvent>(Event<TEvent> @event, CancellationToken cancellationToken) where TEvent : IEvent
    {
        // Check if we're already shutting down
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Skipping processing of event type {EventType} due to cancellation", typeof(TEvent).Name);
            return;
        }
        
        // Create a new scope for dependency resolution
        using var scope = _serviceProvider.CreateScope();
        
        // Get all handlers for this event type
        var handlers = scope.ServiceProvider.GetServices<IConsume<TEvent>>().ToList();
        
        if (handlers.Count == 0)
        {
            _logger.LogDebug("No handlers defined for event of type {EventType}", typeof(TEvent).Name);
            return;
        }
        
        try
        {
            // Create combined token to respect both the stopping token and the provided token
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, 
                _stoppingCts.Token);
                
            // Optimize for single handler case (very common)
            if (handlers.Count == 1)
            {
                try
                {
                    await handlers[0].ConsumeAsync(@event.Data!, linkedCts.Token);
                }
                catch (OperationCanceledException) when (linkedCts.Token.IsCancellationRequested)
                {
                    // Expected during shutdown, log at debug level
                    _logger.LogDebug("Handler {HandlerType} for event {EventType} was cancelled", 
                        handlers[0].GetType().Name, typeof(TEvent).Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in handler {HandlerType} for event {EventType}", 
                        handlers[0].GetType().Name, typeof(TEvent).Name);
                }
                
                return;
            }
            
            // For multiple handlers, use Task.WhenAll for better performance
            // This is more efficient than Parallel.ForEachAsync for small sets of handlers
            var handlerTasks = handlers.Select(handler => 
                InvokeHandlerSafelyAsync(handler, @event.Data!, linkedCts.Token)).ToArray();
                
            await Task.WhenAll(handlerTasks);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // This is expected during shutdown, so just log it at debug level
            _logger.LogDebug("Processing of event type {EventType} was cancelled", typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            // Log any other errors at error level
            _logger.LogError(ex, "Error processing event of type {EventType}", typeof(TEvent).Name);
        }
    }
    
    private async Task InvokeHandlerSafelyAsync<TEvent>(IConsume<TEvent> handler, TEvent eventData, CancellationToken token) 
        where TEvent : IEvent
    {
        try
        {
            await handler.ConsumeAsync(eventData, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Expected during shutdown, log at debug level
            _logger.LogDebug("Handler {HandlerType} for event {EventType} was cancelled", 
                handler.GetType().Name, typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in handler {HandlerType} for event {EventType}", 
                handler.GetType().Name, typeof(TEvent).Name);
        }
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
    }    /// <summary>
    /// Stops the bus and all channel processors
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping InMemoryBus");
        
        try
        {
            // Signal all processing tasks to stop
            if (!_stoppingCts.IsCancellationRequested)
            {
                _stoppingCts.Cancel();
            }
            
            // Wait for all tasks to complete or timeout with progressively longer timeouts
            if (_processingTasks.Any())
            {
                var activeTasks = _processingTasks.Where(t => !t.IsCompleted).ToList();
                if (activeTasks.Any())
                {
                    _logger.LogDebug("Waiting for {Count} active channel processors to complete", activeTasks.Count);
                    
                    // Try a progressive shutdown sequence
                    var timeouts = new[] 
                    { 
                        TimeSpan.FromMilliseconds(500), 
                        TimeSpan.FromSeconds(1), 
                        TimeSpan.FromSeconds(3),
                        TimeSpan.FromSeconds(5)
                    };
                    
                    foreach (var timeout in timeouts)
                    {
                        var remainingTasks = activeTasks.Where(t => !t.IsCompleted).ToList();
                        if (!remainingTasks.Any())
                        {
                            _logger.LogDebug("All channel processors completed successfully");
                            break;
                        }
                        
                        _logger.LogDebug("Still waiting for {Count} tasks, extending timeout to {Timeout}ms", 
                            remainingTasks.Count, timeout.TotalMilliseconds);
                            
                        var timeoutTask = Task.Delay(timeout, cancellationToken);
                        var completedTask = await Task.WhenAny(
                            Task.WhenAll(remainingTasks),
                            timeoutTask
                        );
                        
                        if (completedTask == timeoutTask)
                        {
                            // Continue to the next timeout level
                            continue;
                        }
                        else
                        {
                            // All tasks completed before timeout
                            _logger.LogDebug("All channel processors completed successfully");
                            break;
                        }
                    }
                    
                    // Final check to see if we still have uncompleted tasks
                    var finalRemainingTasks = activeTasks.Where(t => !t.IsCompleted).ToList();
                    if (finalRemainingTasks.Any())
                    {
                        _logger.LogWarning("{Count} channel processors did not complete before final timeout", 
                            finalRemainingTasks.Count);
                            
                        // Log info about the remaining tasks for diagnostics
                        foreach (var task in finalRemainingTasks)
                        {
                            _logger.LogDebug("Task {Id} status: {Status}", 
                                task.Id, task.Status);
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("All channel processors were already completed");
                }
            }
            else
            {
                _logger.LogDebug("No active channel processors to wait for");
            }
            
            // Clear channels to release memory regardless of task completion
            foreach (var channel in _channels.Values)
            {
                // Currently no additional cleanup needed for channels
            }
            
            _channels.Clear();
        }
        catch (Exception ex)
        {
            // Ensure we don't throw exceptions during shutdown
            _logger.LogError(ex, "Error while stopping InMemoryBus");
        }
        finally
        {
            // Always clear collections to free resources, regardless of exceptions
            _channels.Clear();
            _processingTasks.Clear();
            _logger.LogInformation("InMemoryBus stopped");
        }
    }
}
