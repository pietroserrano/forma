using Forma.Abstractions;
using Forma.Core.Abstractions;

namespace Forma.Mediator.Behaviors;

/// <summary>
/// Behavior that executes all registered post-processors for a request.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class RequestPostProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IRequestPostProcessor<TRequest, TResponse>> _postProcessors;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestPostProcessorBehavior{TMessage, TResponse}"/> class.
    /// </summary>
    /// <param name="postProcessors"></param>
    public RequestPostProcessorBehavior(IEnumerable<IRequestPostProcessor<TRequest, TResponse>> postProcessors)
        => _postProcessors = postProcessors;

    /// <summary>
    /// Handles the request and executes all registered post-processors.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="next"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<CancellationToken, Task<TResponse>> next)
    {
        var response = await next(cancellationToken).ConfigureAwait(false);

        foreach (var processor in _postProcessors)
        {
            await processor.ProcessAsync(request, response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
