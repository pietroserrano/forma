namespace Forma.Chains.Configurations;

/// <summary>
/// Definisce le strategie di ordinamento degli handler in una catena.
/// </summary>
public enum ChainOrderStrategy
{
    /// <summary>
    /// Mantiene l'ordine degli handler così come sono stati forniti o rilevati.
    /// </summary>
    AsProvided,
    
    /// <summary>
    /// Ordina gli handler alfabeticamente per nome del tipo.
    /// </summary>
    Alphabetical,

    /// <summary>
    /// Ordina gli handler in ordine alfabetico inverso per nome del tipo.
    /// </summary>
    ReverseAlphabetical,
    
    /// <summary>
    /// Ordina gli handler in base all'attributo di priorità, se presente.
    /// </summary>
    Priority
}
