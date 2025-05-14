using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Tests.Mediator;

public class RequestMediatorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRequestMediator _mediator;

    public RequestMediatorTests()
    {
        var services = new ServiceCollection();

        // Registra i servizi necessari per il mediator
        services.AddRequestMediator(config => { });

        // Aggiungi l'handler di test
        services.AddScoped<IHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped<IHandler<SimpleRequest>, SimpleRequestHandler>();
        services.AddScoped<IHandler<ThrowingRequest, TestResponse>, ThrowingRequestHandler>();
        services.AddScoped<IHandler<CancellableRequest>, CancellableRequestHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IRequestMediator>();
    }

    [Fact]
    public async Task SendAsync_WithResponseType_ReturnsCorrectResponse()
    {
        // Arrange
        var request = new TestRequest { Data = "Test" };

        // Act
        var response = await _mediator.SendAsync<TestResponse>(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("Test result", response.Result);
    }

    [Fact]
    public async Task SendAsync_WithoutResponseType_CompletesSuccessfully()
    {
        // Arrange
        var request = new SimpleRequest { Data = "Simple" };

        // Act
        await _mediator.SendAsync(request);

        // Assert (verifica che non siano state lanciate eccezioni)
        Assert.True(true);
    }

    [Fact]
    public async Task SendAsync_WithObjectRequest_ReturnsCorrectResponse()
    {
        // Arrange
        object request = new TestRequest { Data = "Dynamic" };

        // Act
        var response = await _mediator.SendAsync(request);

        // Assert
        Assert.NotNull(response);
        var typedResponse = response as TestResponse;
        Assert.NotNull(typedResponse);
        Assert.Equal("Dynamic result", typedResponse?.Result);
    }

    [Fact]
    public async Task SendAsync_WithInvalidRequest_ThrowsArgumentException()
    {
        // Arrange
        object request = new InvalidRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _mediator.SendAsync(request));
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.SendAsync<TestResponse>(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.SendAsync(null as SimpleRequest));
#pragma warning restore CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
#pragma warning disable CS8604 // Possible null reference argument.
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.SendAsync(null as object));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Fact]
    public async Task SendAsync_WithThrowingHandler_PropagatesException()
    {
        // Arrange
        var request = new ThrowingRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mediator.SendAsync<TestResponse>(request));

        Assert.Equal("Handler error", exception.Message);
    }

    [Fact]
    public async Task SendAsync_WithMissingHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new MissingHandlerRequest();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mediator.SendAsync<TestResponse>(request));
    }

    [Fact]
    public async Task SendAsync_WithCancellation_HonorsCancellationToken()
    {
        // Arrange
        var request = new CancellableRequest();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancella immediatamente

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _mediator.SendAsync(request, cts.Token));
    }
}

// Classi di supporto per i test
public class TestRequest : IRequest<TestResponse>
{
    public string? Data { get; set; }
}

public class TestResponse
{
    public string? Result { get; set; }
}

public class TestRequestHandler : IHandler<TestRequest, TestResponse>
{
    public Task<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestResponse { Result = $"{request.Data} result" });
    }
}

public class SimpleRequest : IRequest
{
    public string? Data { get; set; }
}

public class SimpleRequestHandler : IHandler<SimpleRequest>
{
    public Task HandleAsync(SimpleRequest request, CancellationToken cancellationToken)
    {
        // No response needed
        return Task.CompletedTask;
    }
}

public class ThrowingRequest : IRequest<TestResponse> { }

public class ThrowingRequestHandler : IHandler<ThrowingRequest, TestResponse>
{
    public Task<TestResponse> HandleAsync(ThrowingRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler error");
    }
}

public class MissingHandlerRequest : IRequest<TestResponse> { }

public class CancellableRequest : IRequest
{
}

// Classe di handler per gestire CancellableRequest
public class CancellableRequestHandler : IHandler<CancellableRequest>
{
    public Task HandleAsync(CancellableRequest request, CancellationToken cancellationToken)
    {
        // Verifica che il token sia rispettato
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

public class InvalidRequest { }
