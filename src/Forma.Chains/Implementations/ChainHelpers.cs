using Forma.Chains.Abstractions;
using Forma.Chains.Configurations;

namespace Forma.Chains.Implementations;

/// <summary>
/// Classe per aiutare nella gestione degli handler nelle catene.
/// </summary>
internal static class ChainHelpers
{
    /// <summary>
    /// Ordina un elenco di handler in base alla strategia di ordinamento specificata.
    /// </summary>
    /// <param name="handlerTypes">I tipi di handler da ordinare.</param>
    /// <param name="orderStrategy">La strategia di ordinamento da utilizzare.</param>
    /// <returns>I tipi di handler ordinati.</returns>
    public static IEnumerable<Type> OrderHandlerTypes(IEnumerable<Type> handlerTypes, ChainOrderStrategy orderStrategy)
    {
        return orderStrategy switch
        {
            ChainOrderStrategy.Alphabetical => handlerTypes.OrderBy(t => t.Name),
            ChainOrderStrategy.Priority => OrderByPriority(handlerTypes),
            _ => handlerTypes // ChainOrderStrategy.AsProvided
        };
    }

    /// <summary>
    /// Ordina i tipi di handler in base all'attributo di priorità.
    /// </summary>
    /// <param name="handlerTypes">I tipi di handler da ordinare.</param>
    /// <returns>I tipi di handler ordinati per priorità.</returns>
    private static IEnumerable<Type> OrderByPriority(IEnumerable<Type> handlerTypes)
    {
        // Per ora implementiamo un ordinamento semplice basato sul nome
        // In futuro si potrà implementare un ordinamento basato su un attributo di priorità
        return handlerTypes.OrderBy(t => t.Name);
    }    /// <summary>
    /// Gestisce il caso in cui non vengono trovati handler.
    /// </summary>
    /// <typeparam name="THandler">Il tipo di handler.</typeparam>
    /// <param name="behavior">Il comportamento da adottare.</param>
    /// <param name="requestType">Il tipo di richiesta.</param>
    /// <returns>Una lista di handler o lancia un'eccezione.</returns>
    /// <exception cref="InvalidOperationException">Lanciata se il comportamento è ThrowException.</exception>
    public static List<THandler> HandleMissingHandlers<THandler>(MissingHandlerBehavior behavior, Type requestType)
    {
        return behavior switch
        {
            MissingHandlerBehavior.ReturnEmpty => CreateEmptyHandler<THandler>(requestType),
            MissingHandlerBehavior.UseDefaultHandler => CreateDefaultHandler<THandler>(requestType),
            _ => throw new InvalidOperationException($"Non sono stati trovati handler per il tipo di richiesta {requestType.Name}.")
        };
    }
    
    /// <summary>
    /// Crea un handler vuoto.
    /// </summary>
    /// <typeparam name="THandler">Il tipo di handler.</typeparam>
    /// <param name="requestType">Il tipo di richiesta.</param>
    /// <returns>Una lista contenente un handler vuoto.</returns>
    private static List<THandler> CreateEmptyHandler<THandler>(Type requestType)
    {
        if (typeof(THandler).IsGenericType)
        {
            var typeArgs = typeof(THandler).GetGenericArguments();
            if (typeArgs.Length == 1)
            {
                // Handler senza risposta
                var handlerType = typeof(EmptyHandler<>).MakeGenericType(typeArgs[0]);
                var handler = Activator.CreateInstance(handlerType);
                return new List<THandler> { (THandler)handler! };
            }
            else if (typeArgs.Length == 2)
            {
                // Handler con risposta
                var handlerType = typeof(EmptyHandler<,>).MakeGenericType(typeArgs[0], typeArgs[1]);
                var handler = Activator.CreateInstance(handlerType);
                return new List<THandler> { (THandler)handler! };
            }
        }
        
        // Fallback: restituisci una lista vuota
        return new List<THandler>();
    }

    /// <summary>
    /// Crea un handler di default.
    /// </summary>
    /// <typeparam name="THandler">Il tipo di handler.</typeparam>
    /// <param name="requestType">Il tipo di richiesta.</param>
    /// <returns>Una lista contenente un handler di default.</returns>
    private static List<THandler> CreateDefaultHandler<THandler>(Type requestType)
    {
        // Questo metodo dovrebbe creare un handler di default
        // Per ora utilizziamo la stessa implementazione di CreateEmptyHandler
        return CreateEmptyHandler<THandler>(requestType);
    }
      /// <summary>
    /// Implementazione di un handler vuoto per la catena di responsabilità.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita.</typeparam>
    private class EmptyHandler<TRequest> : IChainHandler<TRequest> where TRequest : notnull
    {
        public Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task HandleAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void SetNext(IChainHandler<TRequest> next)
        {
            // Non fa nulla, non c'è un handler successivo
        }
        
        public Task NextAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            // Non c'è un handler successivo, quindi terminiamo
            return Task.CompletedTask;
        }
    }
      /// <summary>
    /// Implementazione di un handler vuoto per la catena di responsabilità che restituisce una risposta.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita.</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta che viene restituita.</typeparam>
    private class EmptyHandler<TRequest, TResponse> : IChainHandler<TRequest, TResponse> where TRequest : notnull
    {
        public Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TResponse>(default!);
        }

        public void SetNext(IChainHandler<TRequest, TResponse> next)
        {
            // Non fa nulla, non c'è un handler successivo
        }
        
        public Task<TResponse> NextAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            // Non c'è un handler successivo, quindi terminiamo con valore predefinito
            return Task.FromResult<TResponse>(default!);
        }
    }
}
