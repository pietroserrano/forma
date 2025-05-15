namespace Forma.Core.PubSub.Abstractions;

/// <summary>
/// Interface for a message bus.
/// </summary>
public interface IBus
{
    /// <summary>
    /// Publishes an event to the bus.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
}
