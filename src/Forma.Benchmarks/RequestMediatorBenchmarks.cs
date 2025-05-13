using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator;
using Forma.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace Forma.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RequestMediatorBenchmarks
{    private IRequestMediator _mediator = null!;
    private SimpleRequest _simpleRequest = null!;
    private SimpleRequestWithResponse<string> _requestWithResponse = null!;    [GlobalSetup]
    public void Setup()
    {
        // Configurazione del container di DI
        var services = new ServiceCollection();
        
        // Registrazione degli handler per i nostri request
        services.AddTransient<IHandler<SimpleRequest>, SimpleRequestHandler>();
        services.AddTransient<IHandler<SimpleRequestWithResponse<string>, string>, SimpleRequestWithResponseHandler>();
        
        // Configurazione del mediator usando l'estensione dal test
        services.AddRequestMediator(config => { });
        
        // Costruzione del ServiceProvider
        var serviceProvider = services.BuildServiceProvider();
        
        // Ottenimento dell'istanza di IRequestMediator
        _mediator = serviceProvider.GetRequiredService<IRequestMediator>();
        
        // Inizializzazione delle richieste di test
        _simpleRequest = new SimpleRequest();
        _requestWithResponse = new SimpleRequestWithResponse<string>();
    }

    [Benchmark]
    public async Task SendAsync_SimpleRequest()
    {
        // Test del primo metodo: SendAsync<TRequest>
        await _mediator.SendAsync(_simpleRequest);
    }

    [Benchmark]
    public async Task<string> SendAsync_RequestWithResponse()
    {
        // Test del secondo metodo: SendAsync<TResponse>
        return await _mediator.SendAsync(_requestWithResponse);
    }

    [Benchmark]
    public async Task<object?> SendAsync_Object()
    {
        // Test del terzo metodo: SendAsync(object)
        return await _mediator.SendAsync(_requestWithResponse);
    }
}

// Classi di supporto per i test

public class SimpleRequest : IRequest
{
    public string Data { get; set; } = "Test data";
}

public class SimpleRequestWithResponse<T> : IRequest<T>
{
    public string Data { get; set; } = "Test data";
}

public class SimpleRequestHandler : IHandler<SimpleRequest>
{
    public Task HandleAsync(SimpleRequest request, CancellationToken cancellationToken = default)
    {
        // Simuliamo una semplice elaborazione
        return Task.CompletedTask;
    }
}

public class SimpleRequestWithResponseHandler : IHandler<SimpleRequestWithResponse<string>, string>
{
    public Task<string> HandleAsync(SimpleRequestWithResponse<string> request, CancellationToken cancellationToken = default)
    {
        // Simuliamo una semplice elaborazione con risposta
        return Task.FromResult($"Response: {request.Data}");
    }
}
