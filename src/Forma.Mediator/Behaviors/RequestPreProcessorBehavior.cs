using Forma.Abstractions;
using Forma.Core.Abstractions;

namespace Forma.Mediator.Behaviors;

/// <summary>
/// Behavior that executes all registered pre-processors for a request.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class RequestPreProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IRequestPreProcessor<TRequest>> _preProcessors;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestPreProcessorBehavior{TMessage, TResponse}"/> class.
    /// </summary>
    /// <param name="preProcessors"></param>
    public RequestPreProcessorBehavior(IEnumerable<IRequestPreProcessor<TRequest>> preProcessors)
        => _preProcessors = preProcessors;

    /// <summary>
    /// Handles the request and executes all registered pre-processors.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<CancellationToken, Task<TResponse>> next)
    {
        foreach (var processor in _preProcessors)
        {
            await processor.ProcessAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return await next(cancellationToken).ConfigureAwait(false);
    }
}
