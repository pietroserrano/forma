namespace Forma.Core.PubSub.Abstractions;

/// <summary>
/// Represents the metadata for an event.
/// This metadata can include information such as the correlation ID.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IEventContextAccessor<T> where T : IEvent
{
    /// <summary>
    /// Gets the event associated with the current context.
    /// </summary>
    public Event<T>? Event { get; }

    /// <summary>
    /// Sets the event for the current context.
    /// </summary>
    /// <param name="event"></param>
    void Set(Event<T> @event);
}
