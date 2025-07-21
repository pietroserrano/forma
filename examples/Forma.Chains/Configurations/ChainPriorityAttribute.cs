using System;

namespace Forma.Chains.Configurations;

/// <summary>
/// Attributo per definire la priorità di un handler in una catena.
/// </summary>
public class ChainPriorityAttribute : Attribute
{
    /// <summary>
    /// Inizializza una nuova istanza dell'attributo <see cref="ChainPriorityAttribute"/>.
    /// </summary>
    /// <param name="priority">La priorità dell'handler.</param>
    public ChainPriorityAttribute(int priority)
    {
        Priority = priority;
    }

    /// <summary>
    /// Ottiene la priorità dell'handler.
    /// </summary>
    public int Priority { get; }
}
