using Forma.Chains.Abstractions;

namespace Forma.Chains.Implementations;

/// <summary>
/// Classe base per gli handler nella catena di responsabilità.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita.</typeparam>
public abstract class ChainHandlerBase<TRequest> : IChainHandler<TRequest> where TRequest : notnull
{
    /// <summary>
    /// Il prossimo handler nella catena.
    /// </summary>
    protected IChainHandler<TRequest>? Next { get; private set; }
    
    /// <summary>
    /// Imposta il prossimo handler nella catena.
    /// </summary>
    /// <param name="next">Il prossimo handler nella catena.</param>
    public void SetNext(IChainHandler<TRequest> next) => Next = next;
    
    /// <summary>
    /// Determina se questo handler può gestire la richiesta.
    /// </summary>
    /// <param name="request">La richiesta da valutare.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>True se questo handler può gestire la richiesta, altrimenti false.</returns>
    public abstract Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gestisce la richiesta.
    /// </summary>
    /// <param name="request">La richiesta da gestire.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Task rappresentante l'operazione asincrona.</returns>
    public async Task HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (await CanHandleAsync(request, cancellationToken))
        {
            // Questo handler gestisce la richiesta
            await ProcessRequestAsync(request, cancellationToken);
        }
        else
        {
            // Passa al prossimo handler nella catena
            await NextAsync(request, cancellationToken);
        }
    }
    
    /// <summary>
    /// Passa la richiesta al prossimo handler nella catena.
    /// </summary>
    /// <param name="request">La richiesta da passare.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Task rappresentante l'operazione asincrona.</returns>
    public virtual Task NextAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (Next == null)
        {
            // Nessun altro handler nella catena può gestire la richiesta
            return OnChainEndAsync(request, cancellationToken);
        }
        
        // Passa la richiesta al prossimo handler
        return Next.HandleAsync(request, cancellationToken);
    }
    
    /// <summary>
    /// Elabora la richiesta quando questo handler decide di gestirla.
    /// Deve essere implementato dalle classi derivate.
    /// </summary>
    /// <param name="request">La richiesta da elaborare.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Task rappresentante l'operazione asincrona.</returns>
    protected abstract Task ProcessRequestAsync(TRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Metodo chiamato quando la richiesta raggiunge la fine della catena senza essere gestita.
    /// Può essere sovrascritto per personalizzare il comportamento.
    /// </summary>
    /// <param name="request">La richiesta non gestita.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Task rappresentante l'operazione asincrona.</returns>
    protected virtual Task OnChainEndAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        // Implementazione predefinita: non fa nulla
        return Task.CompletedTask;
    }
}

/// <summary>
/// Classe base per gli handler nella catena di responsabilità che restituiscono una risposta.
/// </summary>
/// <typeparam name="TRequest">Il tipo di richiesta che viene gestita.</typeparam>
/// <typeparam name="TResponse">Il tipo di risposta che viene restituita.</typeparam>
public abstract class ChainHandlerBase<TRequest, TResponse> : IChainHandler<TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// Il prossimo handler nella catena.
    /// </summary>
    protected IChainHandler<TRequest, TResponse>? Next { get; private set; }
    
    /// <summary>
    /// Il valore di risposta predefinito da restituire se nessun handler può gestire la richiesta.
    /// </summary>
    protected TResponse? DefaultResponse { get; set; }
    
    /// <summary>
    /// Imposta il prossimo handler nella catena.
    /// </summary>
    /// <param name="next">Il prossimo handler nella catena.</param>
    public void SetNext(IChainHandler<TRequest, TResponse> next) => Next = next;
    
    /// <summary>
    /// Determina se questo handler può gestire la richiesta.
    /// </summary>
    /// <param name="request">La richiesta da valutare.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>True se questo handler può gestire la richiesta, altrimenti false.</returns>
    public abstract Task<bool> CanHandleAsync(TRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gestisce la richiesta e restituisce una risposta.
    /// </summary>
    /// <param name="request">La richiesta da gestire.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>La risposta elaborata.</returns>
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (await CanHandleAsync(request, cancellationToken))
        {
            // Questo handler gestisce la richiesta
            return await ProcessRequestAsync(request, cancellationToken);
        }
        else
        {
            // Passa al prossimo handler nella catena
            return await NextAsync(request, cancellationToken);
        }
    }
    
    /// <summary>
    /// Passa la richiesta al prossimo handler nella catena e restituisce la risposta.
    /// </summary>
    /// <param name="request">La richiesta da passare.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>La risposta dal prossimo handler nella catena.</returns>
    public virtual async Task<TResponse> NextAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (Next == null)
        {
            // Nessun altro handler nella catena può gestire la richiesta
            return await OnChainEndAsync(request, cancellationToken);
        }
        
        // Passa la richiesta al prossimo handler
        return await Next.HandleAsync(request, cancellationToken);
    }
    
    /// <summary>
    /// Elabora la richiesta quando questo handler decide di gestirla.
    /// Deve essere implementato dalle classi derivate.
    /// </summary>
    /// <param name="request">La richiesta da elaborare.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>La risposta elaborata.</returns>
    protected abstract Task<TResponse> ProcessRequestAsync(TRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Metodo chiamato quando la richiesta raggiunge la fine della catena senza essere gestita.
    /// Può essere sovrascritto per personalizzare il comportamento.
    /// </summary>
    /// <param name="request">La richiesta non gestita.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>La risposta predefinita o un'eccezione se non è stata impostata.</returns>
    protected virtual Task<TResponse> OnChainEndAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (DefaultResponse != null)
        {
            return Task.FromResult(DefaultResponse);
        }
        
        throw new InvalidOperationException($"La richiesta non è stata gestita da nessun handler nella catena e non è stato specificato un valore predefinito.");
    }
}
