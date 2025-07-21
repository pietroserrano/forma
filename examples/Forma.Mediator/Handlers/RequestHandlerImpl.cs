using Forma.Abstractions;
using Forma.Mediator.Handlers.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Mediator.Handlers;

/// <summary>
/// RequestHandlerImpl is a generic class that implements the IMessageHandler interface.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class RequestHandlerImpl<TRequest, TResponse> : RequestHandler<TResponse>
    where TRequest : IRequest<TResponse>
{
        /// <summary>
    /// Initializes a new instance of the <see cref="RequestHandlerImpl{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public RequestHandlerImpl(IServiceProvider serviceProvider) : base(serviceProvider)
    {

    }

    /// <summary>
    /// Handles a message and returns a response.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<object?> Handle(object message,
        CancellationToken cancellationToken) =>
        await HandleAsync((IRequest<TResponse>)message, cancellationToken);

    /// <summary>
    /// Handles a strongly typed message and returns a response.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override Task<TResponse> HandleAsync(IRequest<TResponse> message,
        CancellationToken cancellationToken)
    {
        Task<TResponse> Handler(CancellationToken ct) =>
            _serviceProvider.GetRequiredService<IHandler<TRequest, TResponse>>()
                .HandleAsync((TRequest)message, ct == default ? cancellationToken : ct);

        // Fix: Ensure TRequest is constrained to IRequest<TResponse> to satisfy BuildPipeline's type requirements
        return BuildPipeline((TRequest)message, _serviceProvider, Handler, cancellationToken)(cancellationToken);
    }
}

/// <summary>
/// RequestHandlerImpl is a generic class that implements the IMessageHandler interface.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public class RequestHandlerImpl<TRequest> : RequestHandler
    where TRequest : IRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestHandlerImpl{TRequest}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public RequestHandlerImpl(IServiceProvider serviceProvider) : base(serviceProvider)
    {

    }

    /// <summary>
    /// Gestisce un messaggio come oggetto generico.
    /// </summary>
    /// <param name="message">Il messaggio da gestire.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Un task che rappresenta l'operazione asincrona.</returns>
    public override async Task<object?> Handle(object message,
        CancellationToken cancellationToken) =>
        await HandleAsync((IRequest)message, cancellationToken);

    /// <summary>
    /// Gestisce un messaggio fortemente tipizzato senza risultato specifico.
    /// </summary>
    /// <param name="request">Il messaggio da gestire.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Un task che rappresenta l'operazione asincrona.</returns>
    public override Task<object?> HandleAsync(IRequest request,
        CancellationToken cancellationToken)
    {
        async Task<object?> Handler(CancellationToken ct)
        {
            await _serviceProvider.GetRequiredService<IHandler<TRequest>>()
                .HandleAsync((TRequest)request, ct == default ? cancellationToken : ct);
            return default;
        }

        // Fix: Ensure TRequest is constrained to IRequest to satisfy BuildPipeline's type requirements
        return BuildPipeline((TRequest)request, _serviceProvider, Handler, cancellationToken)(cancellationToken);
    }
}