namespace Forma.Abstractions;

/// <summary>
/// Behavior that is executed before and/or after a handler.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// Handles the request and returns a response.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task<TResponse>> next);
}
