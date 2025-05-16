namespace Forma.Chains.Configurations;

/// <summary>
/// Definisce il comportamento da adottare quando non vengono trovati handler per una catena.
/// </summary>
public enum MissingHandlerBehavior
{
    /// <summary>
    /// Lancia un'eccezione se non vengono trovati handler.
    /// </summary>
    ThrowException,
    
    /// <summary>
    /// Restituisce una catena vuota senza handler.
    /// </summary>
    ReturnEmpty,
    
    /// <summary>
    /// Utilizza un handler di default che non fa nulla.
    /// </summary>
    UseDefaultHandler
}
