using System;

namespace Forma.Core.PubSub.Abstractions;

/// <summary>
/// Interface for consuming messages from a message bus.
/// This interface is used to define a contract for classes that will consume messages from the bus.
/// </summary>
public interface IConsume<in TMessage> where TMessage : IEvent
{
    /// <summary>
    /// Consumes a message from the bus.
    /// </summary>
    /// <param name="message">The message to consume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConsumeAsync(TMessage message, CancellationToken cancellationToken = default);
}
