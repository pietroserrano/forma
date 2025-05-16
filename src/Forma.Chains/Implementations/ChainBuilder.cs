using Forma.Chains.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Chains.Implementations;

/// <summary>
/// Implementazione di default del builder per catene di handler.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
public class ChainBuilder<TRequest> : IChainBuilder<TRequest> where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<Type> _handlerTypes;
    
    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ChainBuilder{TRequest}"/>.
    /// </summary>
    /// <param name="serviceProvider">Il provider di servizi.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena.</param>
    public ChainBuilder(IServiceProvider serviceProvider, IEnumerable<Type> handlerTypes)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _handlerTypes = handlerTypes ?? throw new ArgumentNullException(nameof(handlerTypes));
    }
    
    /// <summary>
    /// Costruisce la catena di handler e restituisce il primo handler.
    /// </summary>
    /// <returns>Il primo handler nella catena.</returns>
    public IChainHandler<TRequest> Build()
    {
        var handlerInstances = new List<IChainHandler<TRequest>>();
        
        // Crea le istanze degli handler
        foreach (var handlerType in _handlerTypes)
        {
            if (_serviceProvider.GetService(handlerType) is IChainHandler<TRequest> handler)
            {
                handlerInstances.Add(handler);
            }
            else
            {
                throw new InvalidOperationException($"Il tipo {handlerType.Name} non è un handler valido per {typeof(TRequest).Name}.");
            }
        }
        
        if (handlerInstances.Count == 0)
        {
            throw new InvalidOperationException($"Non sono stati trovati handler per il tipo di richiesta {typeof(TRequest).Name}.");
        }
        
        // Collega gli handler in una catena
        for (int i = 0; i < handlerInstances.Count - 1; i++)
        {
            handlerInstances[i].SetNext(handlerInstances[i + 1]);
        }
        
        // Restituisci il primo handler nella catena
        return handlerInstances[0];
    }
}

/// <summary>
/// Implementazione di default del builder per catene di handler che restituiscono una risposta.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
/// <typeparam name="TResponse">Il tipo di risposta che viene restituita dalla catena.</typeparam>
public class ChainBuilder<TRequest, TResponse> : IChainBuilder<TRequest, TResponse> where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<Type> _handlerTypes;
    
    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ChainBuilder{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="serviceProvider">Il provider di servizi.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena.</param>
    public ChainBuilder(IServiceProvider serviceProvider, IEnumerable<Type> handlerTypes)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _handlerTypes = handlerTypes ?? throw new ArgumentNullException(nameof(handlerTypes));
    }
    
    /// <summary>
    /// Costruisce la catena di handler e restituisce il primo handler.
    /// </summary>
    /// <returns>Il primo handler nella catena.</returns>
    public IChainHandler<TRequest, TResponse> Build()
    {
        var handlerInstances = new List<IChainHandler<TRequest, TResponse>>();
        
        // Crea le istanze degli handler
        foreach (var handlerType in _handlerTypes)
        {
            if (_serviceProvider.GetService(handlerType) is IChainHandler<TRequest, TResponse> handler)
            {
                handlerInstances.Add(handler);
            }
            else
            {
                throw new InvalidOperationException($"Il tipo {handlerType.Name} non è un handler valido per {typeof(TRequest).Name} con risposta {typeof(TResponse).Name}.");
            }
        }
        
        if (handlerInstances.Count == 0)
        {
            throw new InvalidOperationException($"Non sono stati trovati handler per il tipo di richiesta {typeof(TRequest).Name} con risposta {typeof(TResponse).Name}.");
        }
        
        // Collega gli handler in una catena
        for (int i = 0; i < handlerInstances.Count - 1; i++)
        {
            handlerInstances[i].SetNext(handlerInstances[i + 1]);
        }
        
        // Restituisci il primo handler nella catena
        return handlerInstances[0];
    }
}
