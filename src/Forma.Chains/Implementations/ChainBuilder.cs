using Forma.Chains.Abstractions;
using Forma.Chains.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Chains.Implementations;

/// <summary>
/// Implementazione estesa del builder per catene di handler che supporta configurazioni avanzate.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
public class ChainBuilder<TRequest> : IChainBuilder<TRequest> where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<Type> _handlerTypes;
    private readonly ChainConfiguration _configuration;
    
    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ChainBuilder{TRequest}"/>.
    /// </summary>
    /// <param name="serviceProvider">Il provider di servizi.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena.</param>
    /// <param name="configuration">La configurazione della catena.</param>
    public ChainBuilder(IServiceProvider serviceProvider, IEnumerable<Type> handlerTypes, ChainConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _handlerTypes = handlerTypes ?? throw new ArgumentNullException(nameof(handlerTypes));
        _configuration = configuration ?? new ChainConfiguration();
    }
    
    /// <summary>
    /// Costruisce la catena di handler e restituisce il primo handler.
    /// </summary>
    /// <returns>Il primo handler nella catena.</returns>
    public ChainInvoker<TRequest> Build()
    {
        var handlerInstances = new List<IChainHandler<TRequest>>();
        
        // Ordina i tipi di handler in base alla strategia di ordinamento
        var orderedHandlerTypes = ChainHelpers.OrderHandlerTypes(_handlerTypes, _configuration.OrderStrategy);
        
        // Crea le istanze degli handler
        foreach (var handlerType in orderedHandlerTypes)
        {
            if (_serviceProvider.GetService(handlerType) is IChainHandler<TRequest> handler)
            {
                // Applica la validazione se specificata
                if (_configuration.HandlerValidator == null || _configuration.HandlerValidator(handler))
                {
                    handlerInstances.Add(handler);
                }
            }
        }        // Gestisci il caso di nessun handler trovato
        if (handlerInstances.Count == 0)
        {           
            handlerInstances = ChainHelpers.HandleMissingHandlers<TRequest>(
                _configuration.MissingHandlerBehavior, 
                typeof(TRequest));
        }
        
        // // Collega gli handler in una catena
        // for (int i = 0; i < handlerInstances.Count - 1; i++)
        // {
        //     handlerInstances[i].SetNext(handlerInstances[i + 1]);
        // }
        
        // Restituisci il primo handler nella catena
        return new ChainInvoker<TRequest>(handlerInstances);
    }
}

/// <summary>
/// Implementazione estesa del builder per catene di handler che restituiscono una risposta e supportano configurazioni avanzate.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
/// <typeparam name="TResponse">Il tipo di risposta che viene restituita dalla catena.</typeparam>
public class ChainBuilder<TRequest, TResponse> : IChainBuilder<TRequest, TResponse> where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<Type> _handlerTypes;
    private readonly ChainConfiguration _configuration;
    
    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ChainBuilder{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="serviceProvider">Il provider di servizi.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena.</param>
    /// <param name="configuration">La configurazione della catena.</param>
    public ChainBuilder(IServiceProvider serviceProvider, IEnumerable<Type> handlerTypes, ChainConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _handlerTypes = handlerTypes ?? throw new ArgumentNullException(nameof(handlerTypes));
        _configuration = configuration ?? new ChainConfiguration();
    }
    
    /// <summary>
    /// Costruisce la catena di handler e restituisce il primo handler.
    /// </summary>
    /// <returns>Il primo handler nella catena.</returns>
    public ChainInvoker<TRequest, TResponse> Build()
    {
        var handlerInstances = new List<IChainHandler<TRequest, TResponse>>();
        
        // Ordina i tipi di handler in base alla strategia di ordinamento
        var orderedHandlerTypes = ChainHelpers.OrderHandlerTypes(_handlerTypes, _configuration.OrderStrategy);
        
        // Crea le istanze degli handler
        foreach (var handlerType in orderedHandlerTypes)
        {
            if (_serviceProvider.GetService(handlerType) is IChainHandler<TRequest, TResponse> handler)
            {
                // Applica la validazione se specificata
                if (_configuration.HandlerValidator == null || _configuration.HandlerValidator(handler))
                {
                    handlerInstances.Add(handler);
                }
            }
        }
          // Gestisci il caso di nessun handler trovato
        if (handlerInstances.Count == 0)
        {
            handlerInstances = ChainHelpers.HandleMissingHandlers<TRequest, TResponse>(
                _configuration.MissingHandlerBehavior, 
                typeof(TRequest));
        }

        ArgumentOutOfRangeException.ThrowIfZero(handlerInstances.Count, nameof(handlerInstances));
        
        // // Collega gli handler in una catena
        // for (int i = 0; i < handlerInstances.Count - 1; i++)
        // {
        //     handlerInstances[i].SetNext(handlerInstances[i + 1]);
        // }
        
        // Restituisci il primo handler nella catena
        return new ChainInvoker<TRequest, TResponse>(handlerInstances);
    }
}
