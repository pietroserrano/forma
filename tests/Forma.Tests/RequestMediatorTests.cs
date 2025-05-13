using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator;
using Forma.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

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

public class InvalidRequest { }
