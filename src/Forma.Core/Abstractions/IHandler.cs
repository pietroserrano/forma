namespace Forma.Abstractions;

/// <summary>
/// Handles a request and returns a response.
/// </summary>
public interface IHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request and returns a response.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles a request that does not expect a response.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public interface IHandler<in TRequest> where TRequest : IRequest
{
    /// <summary>
    /// Handles the request and returns a response.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}