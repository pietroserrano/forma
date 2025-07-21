namespace Forma.Core.Abstractions;

/// <summary>
/// Pre-processor that is executed before a request is handled. 
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public interface IRequestPreProcessor<in TRequest> where TRequest : notnull
{
    /// <summary>
    /// Processes the request before it is handled.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ProcessAsync(TRequest message, CancellationToken cancellationToken);
}
