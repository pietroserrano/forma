using Forma.Chains.Abstractions;

/// <summary>
/// Classe helper per invocare una chain passando la funzione next direttamente.
/// Gestisce l'avanzamento degli handler forniti tramite IEnumerable.
/// </summary>
public class ChainInvoker<TRequest, TResponse> : IChainHandler<TRequest, TResponse> where TRequest : notnull
{
    private readonly IReadOnlyList<IChainHandler<TRequest, TResponse>> _handlers;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ChainInvoker{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="handlers"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ChainInvoker(IEnumerable<IChainHandler<TRequest, TResponse>> handlers)
    {
        _handlers = handlers?.ToList() ?? [];
    }

    /// <summary>
    /// Controlla se l'handler può gestire la richiesta.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Gestisce la richiesta e restituisce una risposta.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="next"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TResponse> HandleAsync(TRequest request, Func<CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        return HandleInternalAsync(0, request, cancellationToken);
    }

    private async Task<TResponse> HandleInternalAsync(int index, TRequest request, CancellationToken cancellationToken)
    {
        if (index >= _handlers.Count)
            return default!;

        var handler = _handlers[index];
        if (await handler.CanHandleAsync(request, cancellationToken))
            return await handler.HandleAsync(
                request,
                next: ct => HandleInternalAsync(index + 1, request, ct),
                cancellationToken
            );
        else
            return await HandleInternalAsync(index + 1, request, cancellationToken);
    }
}

/// <summary>
/// Classe helper per invocare una chain passando la funzione next direttamente.
/// </summary>
/// <typeparam name="TRequest"></typeparam>

public class ChainInvoker<TRequest> : IChainHandler<TRequest> where TRequest : notnull
{
    private readonly IReadOnlyList<IChainHandler<TRequest>> _handlers;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ChainInvoker{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="handlers"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ChainInvoker(IEnumerable<IChainHandler<TRequest>> handlers)
    {
        _handlers = handlers?.ToList() ?? [];
    }

    /// <summary>
    /// Controlla se l'handler può gestire la richiesta.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Gestisce la richiesta e restituisce una risposta.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="next"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task HandleAsync(TRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        return HandleInternalAsync(0, request, cancellationToken);
    }

    private async Task HandleInternalAsync(int index, TRequest request, CancellationToken cancellationToken)
    {
        if(index >= _handlers.Count)
            return;
        
        var handler = _handlers[index];
        if (await handler.CanHandleAsync(request, cancellationToken))
            await handler.HandleAsync(
                request,
                next: ct => HandleInternalAsync(index + 1, request, ct),
                cancellationToken
            );
        else
            await HandleInternalAsync(index + 1, request, cancellationToken);
    }
}