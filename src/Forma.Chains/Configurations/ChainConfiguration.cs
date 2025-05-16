using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Chains.Configurations;

/// <summary>
/// Configurazione per la catena di responsabilità.
/// </summary>
public class ChainConfiguration
{
    /// <summary>
    /// La durata del servizio ChainBuilder.
    /// </summary>
    public ServiceLifetime ChainBuilderLifetime { get; set; } = ServiceLifetime.Singleton;
    
    /// <summary>
    /// Gli assembly da utilizzare per fare lo scan delle chain.
    /// </summary>
    public Assembly[] Assemblies { get; set; } = [];
    
    /// <summary>
    /// Un filtro opzionale per i tipi di handler da includere nella catena.
    /// </summary>
    public Func<Type, bool>? HandlerTypeFilter { get; set; }
    
    /// <summary>
    /// La strategia per l'ordinamento degli handler nella catena.
    /// Default: Mantiene l'ordine specificato o l'ordine di rilevamento.
    /// </summary>
    public ChainOrderStrategy OrderStrategy { get; set; } = ChainOrderStrategy.AsProvided;
    
    /// <summary>
    /// Determina il comportamento quando non vengono trovati handler per una catena.
    /// </summary>
    public MissingHandlerBehavior MissingHandlerBehavior { get; set; } = MissingHandlerBehavior.ThrowException;
    
    /// <summary>
    /// Determina se abilitare il caching delle catene costruite.
    /// </summary>
    public bool EnableCaching { get; set; } = false;
    
    /// <summary>
    /// Un validatore opzionale per gli handler prima di inserirli nella catena.
    /// </summary>
    public Func<object, bool>? HandlerValidator { get; set; }
}
