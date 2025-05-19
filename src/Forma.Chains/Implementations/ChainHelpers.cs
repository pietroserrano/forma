using System.Reflection;
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
            ChainOrderStrategy.ReverseAlphabetical => handlerTypes.OrderByDescending(t => t.Name),
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
        var withPriority = handlerTypes
            .Select(t => new { Type = t, Attr = t.GetCustomAttribute<ChainPriorityAttribute>() })
            .Where(x => x.Attr != null)
            .OrderBy(x => x.Attr!.Priority)
            .Select(x => x.Type);

        var withoutPriority = handlerTypes
            .Where(t => t.GetCustomAttribute<ChainPriorityAttribute>() == null)
            .OrderBy(t => t.Name);

        return withPriority.Concat(withoutPriority);
    }

    /// <summary>
    /// Gestisce il caso in cui non vengono trovati handler.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta.</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta.</typeparam>
    /// <param name="behavior">Il comportamento da adottare.</param>
    /// <param name="requestType">Il tipo di richiesta.</param>
    /// <returns>Una lista di handler o lancia un'eccezione.</returns>
    /// <exception cref="InvalidOperationException">Lanciata se il comportamento è ThrowException.</exception>
    public static List<IChainHandler<TRequest, TResponse?>> HandleMissingHandlers<TRequest, TResponse>(MissingHandlerBehavior behavior, Type requestType)
        where TRequest : notnull
    {
        return behavior switch
        {
            MissingHandlerBehavior.ReturnEmpty => [new EmptyChainHandler<TRequest, TResponse>()],
            MissingHandlerBehavior.UseDefaultHandler => CreateDefaultHandler<TRequest, TResponse?>(requestType),
            _ => throw new InvalidOperationException($"Non sono stati trovati handler per il tipo di richiesta {requestType.Name}.")
        };
    }

    public static List<IChainHandler<TRequest>> HandleMissingHandlers<TRequest>(MissingHandlerBehavior behavior, Type requestType)
        where TRequest : notnull
    {
        return behavior switch
        {
            MissingHandlerBehavior.ReturnEmpty => [new EmptyChainHandler<TRequest>()],
            MissingHandlerBehavior.UseDefaultHandler => CreateDefaultHandler<TRequest>(requestType),
            _ => throw new InvalidOperationException($"Non sono stati trovati handler per il tipo di richiesta {requestType.Name}.")
        };
    }

    /// <summary>
    /// Crea un handler di default.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta.</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta.</typeparam>
    /// <param name="requestType">Il tipo di richiesta.</param>
    /// <returns>Una lista contenente un handler di default.</returns>
    private static List<IChainHandler<TRequest, TResponse?>> CreateDefaultHandler<TRequest, TResponse>(Type requestType)
        where TRequest : notnull
    {
        //TODO: Implementare la logica per creare un handler di default
        // Questo metodo dovrebbe creare un handler di default
        // Per ora utilizziamo la stessa implementazione di CreateEmptyHandler
        return [new EmptyChainHandler<TRequest, TResponse>()];
    }
    
        /// <summary>
    /// Crea un handler di default.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta.</typeparam>
    /// <param name="requestType">Il tipo di richiesta.</param>
    /// <returns>Una lista contenente un handler di default.</returns>
    private static List<IChainHandler<TRequest>> CreateDefaultHandler<TRequest>(Type requestType)
        where TRequest : notnull
    {
        //TODO: Implementare la logica per creare un handler di default
        // Questo metodo dovrebbe creare un handler di default
        // Per ora utilizziamo la stessa implementazione di CreateEmptyHandler
        return [new EmptyChainHandler<TRequest>()];
    }
    
}
