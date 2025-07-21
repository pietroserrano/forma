namespace Forma.Chains.Abstractions;

/// <summary>
/// Rappresenta un handler nella catena di responsabilità.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita.</typeparam>
public interface IChainHandler<TRequest> where TRequest : notnull
{
    /// <summary>
    /// Determina se questo handler può gestire la richiesta.
    /// </summary>
    /// <param name="request">La richiesta da valutare.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>True se questo handler può gestire la richiesta, altrimenti false.</returns>
    Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gestisce la richiesta.
    /// </summary>
    /// <param name="request">La richiesta da gestire.</param>
    /// <param name="next">Funzione per passare al prossimo handler nella catena.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Task rappresentante l'operazione asincrona.</returns>
    Task HandleAsync(TRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rappresenta un handler nella catena di responsabilità che restituisce una risposta.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita.</typeparam>
/// <typeparam name="TResponse">Il tipo di risposta che viene restituita.</typeparam>
public interface IChainHandler<TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// Determina se questo handler può gestire la richiesta.
    /// </summary>
    /// <param name="request">La richiesta da valutare.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>True se questo handler può gestire la richiesta, altrimenti false.</returns>
    Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gestisce la richiesta e restituisce una risposta.
    /// </summary>
    /// <param name="request">La richiesta da gestire.</param>
    /// <param name="next">Funzione per passare al prossimo handler nella catena.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>La risposta elaborata.</returns>
    Task<TResponse> HandleAsync(TRequest request, Func<CancellationToken, Task<TResponse?>> next, CancellationToken cancellationToken = default);
    
    // /// <summary>
    // /// Passa la richiesta al prossimo handler nella catena e restituisce la risposta.
    // /// </summary>
    // /// <param name="request">La richiesta da passare.</param>
    // /// <param name="cancellationToken">Token di cancellazione.</param>
    // /// <returns>La risposta dal prossimo handler nella catena.</returns>
    // Task<TResponse> NextAsync(TRequest request, CancellationToken cancellationToken = default);
    
    // /// <summary>
    // /// Imposta il prossimo handler nella catena.
    // /// </summary>
    // /// <param name="next">Il prossimo handler nella catena.</param>
    // void SetNext(IChainHandler<TRequest, TResponse> next);
}

/// <summary>
/// Definisce un costruttore di catene di responsabilità.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
public interface IChainBuilder<TRequest> where TRequest : notnull
{
    /// <summary>
    /// Costruisce la catena di handler e restituisce il primo handler.
    /// </summary>
    /// <returns>Il primo handler nella catena.</returns>
    IChainInvoker<TRequest> Build(object? key);
}

/// <summary>
/// Definisce un costruttore di catene di responsabilità che restituiscono una risposta.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
/// <typeparam name="TResponse">Il tipo di risposta che viene restituita dalla catena.</typeparam>
public interface IChainBuilder<TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// Costruisce la catena di handler e restituisce il primo handler.
    /// </summary>
    /// <returns>Il primo handler nella catena.</returns>
    IChainInvoker<TRequest, TResponse> Build(object? key);
}
