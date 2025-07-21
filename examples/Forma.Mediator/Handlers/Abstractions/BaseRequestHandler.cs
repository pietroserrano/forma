using Forma.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Mediator.Handlers.Abstractions;

/// <summary>
/// Classe base per la gestione delle richieste.
/// </summary>
public abstract class BaseRequestHandler
{
    /// <summary>
    /// Provider di servizi per la risoluzione delle dipendenze.
    /// </summary>
    protected readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Costruttore per inizializzare il provider di servizi.
    /// </summary>
    /// <param name="serviceProvider">Il provider di servizi.</param>
    protected BaseRequestHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gestisce un messaggio generico come oggetto.
    /// </summary>
    /// <param name="message">Il messaggio da gestire.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Un task con il risultato dell'elaborazione.</returns>
    public abstract Task<object?> Handle(object message,
        CancellationToken cancellationToken);

    /// <summary>
    /// Costruisce una pipeline di comportamenti per l'elaborazione del messaggio.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di messaggio.</typeparam>
    /// <typeparam name="TResponse">Il tipo di risultato.</typeparam>
    /// <param name="request">Il messaggio da elaborare.</param>
    /// <param name="serviceProvider">Provider di servizi.</param>
    /// <param name="handlerCallback">Il callback del gestore finale.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Un delegato che esegue la pipeline completa.</returns>
    protected Func<CancellationToken, Task<TResponse>> BuildPipeline<TRequest, TResponse>(
        TRequest request,
        IServiceProvider serviceProvider,
        Func<CancellationToken, Task<TResponse>> handlerCallback,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        return serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .Aggregate(handlerCallback,
                (next, pipeline) => ct => pipeline.HandleAsync(
                    request,
                    ct == default ? cancellationToken : ct,
                    next));
    }
}
