namespace Forma.Core.PubSub.Abstractions;

/// <summary>
/// Represents the metadata for an event.
/// </summary>
/// <param name="CorrelationId"></param>
public record EventMetadata(string CorrelationId);
/// <summary>
/// Represents an event with associated data and metadata.
/// This record is used to encapsulate the data and metadata of an event.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Data"></param>
/// <param name="Metadata"></param>
public record Event<T>(T? Data, EventMetadata? Metadata = default) where T : IEvent;
