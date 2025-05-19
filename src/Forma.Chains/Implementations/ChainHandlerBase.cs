using Forma.Chains.Abstractions;

/// <summary>
/// Interfaccia per invocare una catena di handler.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public interface IChainInvoker<TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// Gestisce la richiesta e restituisce una risposta.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TResponse?> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interfaccia per invocare una catena di handler.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public interface IChainInvoker<TRequest> where TRequest : notnull
{
    /// <summary>
    /// Gestisce la richiesta e restituisce una risposta.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Classe helper per invocare una chain passando la funzione next direttamente.
/// Gestisce l'avanzamento degli handler forniti tramite IEnumerable.
/// </summary>
internal class ChainInvoker<TRequest, TResponse> : IChainInvoker<TRequest, TResponse> 
    where TRequest : notnull
{
    private readonly IReadOnlyList<IChainHandler<TRequest, TResponse?>> _handlers;
    private readonly object? _key;

    public ChainInvoker(object? key, IEnumerable<IChainHandler<TRequest, TResponse?>> handlers)
    {
        _handlers = handlers?.ToList() ?? [];
        _key = key;
    }

    /// <inheritdoc/>
    public Task<TResponse?> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return HandleInternalAsync(0, request, cancellationToken);
    }

    private async Task<TResponse?> HandleInternalAsync(int index, TRequest request, CancellationToken cancellationToken)
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


internal class ChainInvoker<TRequest> : IChainInvoker<TRequest> 
    where TRequest : notnull
{
    private readonly IReadOnlyList<IChainHandler<TRequest>> _handlers;
    private readonly object? _key;

    /// <summary>
    /// Inizializza una nuova istanza della classe <see cref="ChainInvoker{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="handlers"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ChainInvoker(object? key, IEnumerable<IChainHandler<TRequest>> handlers)
    {
        _handlers = handlers?.ToList() ?? [];
        _key = key;
    }

    /// <inheritdoc/>
    public Task HandleAsync(TRequest request, CancellationToken cancellationToken = default)
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