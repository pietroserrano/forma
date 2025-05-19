using Forma.Chains.Abstractions;
using Forma.Chains.Configurations;

namespace Forma.Tests.Chains;

// Classi di supporto condivise per tutti i test
public class TestRequest
{
    public int Value { get; set; }
    public List<string>? Results { get; set; }
}

public class TestResponse
{
    public string? Result { get; set; }
}

// Handler per i test di base
public class FirstHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("FirstHandler");
        return next(cancellationToken);
    }
}

public class SecondHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
    
    public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("SecondHandler");
        return next(cancellationToken);
    }
}

public class ThirdHandler : IChainHandler<TestRequest>
{
    public Task<bool> CanHandleAsync(TestRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task HandleAsync(TestRequest request, Func<CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        request.Results?.Add("ThirdHandler");
        return next(cancellationToken);
    }
}
