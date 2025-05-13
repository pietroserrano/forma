using Forma.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Mediator.Handlers.Abstractions;

/// <summary>
/// Base class for handling requests.
/// </summary>
/// <typeparam name="TResponse"></typeparam>
/// <param name="ServiceProvider"></param>
public abstract class RequestHandler<TResponse>(IServiceProvider ServiceProvider) : BaseRequestHandler(ServiceProvider)
{
    /// <summary>
    /// Handles the request and returns a response.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract Task<TResponse> Handle(IRequest<TResponse> message,
        CancellationToken cancellationToken);
}

/// <summary>
/// Base class for handling requests without a specific result.
/// </summary>
/// <param name="ServiceProvider"></param>
public abstract class RequestHandler(IServiceProvider ServiceProvider) : BaseRequestHandler(ServiceProvider)
{
    /// <summary>
    /// Gestisce un messaggio fortemente tipizzato senza risultato specifico.
    /// </summary>
    /// <param name="message">Il messaggio da gestire.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Un task che rappresenta l'operazione asincrona.</returns>
    public abstract Task<object?> Handle(IRequest message,
        CancellationToken cancellationToken);
}