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
    /// Gestisce la richiesta senza fare nulla.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="next"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task HandleAsync(TRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Implementazione vuota di un handler per la catena di responsabilità che restituisce una risposta.
/// Utilizzato quando MissingHandlerBehavior è impostato su ReturnEmpty.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita.</typeparam>
/// <typeparam name="TResponse">Il tipo di risposta che viene restituita.</typeparam>
internal class EmptyChainHandler<TRequest, TResponse> : IChainHandler<TRequest, TResponse?> where TRequest : notnull
{
    /// <summary>
    /// Restituisce sempre false, in quanto questo handler non gestisce alcuna richiesta.
    /// </summary>
    public Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<TResponse?> HandleAsync(TRequest request, Func<CancellationToken, Task<TResponse?>> next, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<TResponse?>(default!);
    }
}
