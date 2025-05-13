using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator.Handlers;
using Forma.Mediator.Handlers.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Mediator;

/// <summary>
/// 
/// </summary>
/// <param name="ServiceProvider"></param>
public class RequestMediator(IServiceProvider ServiceProvider) : IRequestMediator
{
    /// <summary>
    /// Sends a request and returns a response.
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapperType = typeof(RequestHandlerImpl<,>).MakeGenericType(request.GetType(), typeof(TResponse));

        var handler = (RequestHandler<TResponse>)ServiceProvider.GetRequiredService(wrapperType);

        return handler.Handle(request, cancellationToken);
    }

    /// <summary>
    /// Sends a request and returns a response.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapperType = typeof(RequestHandlerImpl<>).MakeGenericType(request.GetType());

        var handler = (RequestHandler)ServiceProvider.GetRequiredService(wrapperType);

        return handler.Handle(request, cancellationToken);
    }

    /// <summary>
    /// Sends a request and returns a response.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Task<object?> SendAsync(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Type wrapperType;
        var requestType = request.GetType();

        var requestInterfaceType = requestType.GetInterfaces().FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
        if (requestInterfaceType is null)
        {
            requestInterfaceType = requestType.GetInterfaces().FirstOrDefault(static i => i == typeof(IRequest));
            if (requestInterfaceType is null)
            {
                throw new ArgumentException($"{requestType.Name} does not implement {nameof(IRequest)}", nameof(request));
            }

            wrapperType = typeof(RequestHandlerImpl<>).MakeGenericType(requestType);
        }
        else
        {
            var responseType = requestInterfaceType.GetGenericArguments()[0];
            wrapperType = typeof(RequestHandlerImpl<,>).MakeGenericType(requestType, responseType);
        }

        var handler = (BaseRequestHandler)ServiceProvider.GetRequiredService(wrapperType);

        return handler.Handle(request, cancellationToken);
    }
}
