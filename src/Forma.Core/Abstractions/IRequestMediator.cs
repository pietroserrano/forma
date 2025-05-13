using Forma.Abstractions;

namespace Forma.Core.Abstractions;

/// <summary>
/// Interface for a request mediator.
/// </summary>
public interface IRequestMediator
{
    /// <summary>
    /// Sends a request and returns a response.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<object?> SendAsync(object request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request and returns a response. 
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;

    /// <summary>
    /// Sends a request and returns a response.
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
