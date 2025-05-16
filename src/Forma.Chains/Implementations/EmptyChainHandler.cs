using Forma.Chains.Abstractions;

namespace Forma.Chains.Implementations;

/// <summary>
/// Implementazione vuota di un handler per la catena di responsabilità.
/// Utilizzato quando MissingHandlerBehavior è impostato su ReturnEmpty.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita.</typeparam>
internal class EmptyChainHandler<TRequest> : IChainHandler<TRequest> where TRequest : notnull
{
    /// <summary>
    /// Restituisce sempre false, in quanto questo handler non gestisce alcuna richiesta.
    /// </summary>
    public Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Gestisce la richiesta non facendo nulla.
    /// </summary>
    public Task HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task NextAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Imposta il prossimo handler nella catena (non utilizzato in questa implementazione).
    /// </summary>
    public void SetNext(IChainHandler<TRequest> next)
    {
        // Non fa nulla, non c'è un handler successivo
    }
}

/// <summary>
/// Implementazione vuota di un handler per la catena di responsabilità che restituisce una risposta.
/// Utilizzato quando MissingHandlerBehavior è impostato su ReturnEmpty.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita.</typeparam>
/// <typeparam name="TResponse">Il tipo di risposta che viene restituita.</typeparam>
internal class EmptyChainHandler<TRequest, TResponse> : IChainHandler<TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// Restituisce sempre false, in quanto questo handler non gestisce alcuna richiesta.
    /// </summary>
    public Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Gestisce la richiesta restituendo il valore predefinito per il tipo TResponse.
    /// </summary>
    public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<TResponse>(default!);
    }

    public Task<TResponse> NextAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<TResponse>(default!);
    }

    /// <summary>
    /// Imposta il prossimo handler nella catena (non utilizzato in questa implementazione).
    /// </summary>
    public void SetNext(IChainHandler<TRequest, TResponse> next)
    {
        // Non fa nulla, non c'è un handler successivo
    }
}
