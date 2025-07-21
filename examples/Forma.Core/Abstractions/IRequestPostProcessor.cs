namespace Forma.Core.Abstractions;

/// <summary>
/// Post-processor that is executed after a request is handled.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public interface IRequestPostProcessor<in TRequest, in TResponse> where TRequest : notnull
{
    /// <summary>
    /// Processes the request after it is handled.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="response"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ProcessAsync(TRequest message, TResponse response, CancellationToken cancellationToken);
}
