using Forma.Abstractions;
using Forma.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Tests.Mediator;

public class DependencyInjectionTests
{
    // Tipi di test per simulare registrazioni
    public interface ITestInterface<T> { }
    public class TestImplementation : ITestInterface<string> { }
    public class TestImplementation2 : ITestInterface<int> { }
    public class TestImplementation3 : ITestInterface<bool> { }

    [Fact]
    public void AddAllGenericImplementations_RegistersCorrectImplementations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAllGenericImplementations(typeof(ITestInterface<>), ServiceLifetime.Scoped, null, typeof(TestImplementation).Assembly);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITestInterface<string>));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(TestImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddAllGenericImplementations_ThrowsOnNonInterface()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddAllGenericImplementations(typeof(TestImplementation)));
    }

    [Fact]
    public void AddMessageMediator_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRequestMediator(config => { });

        // Assert
        Assert.NotEmpty(services);
        Assert.Contains(services, sd => sd.ServiceType.Name == "IRequestMediator");
        Assert.Contains(services, sd => sd.ServiceType.Name.StartsWith("RequestHandlerImpl"));
    }

    [Fact]
    public void MessageMediatorConfiguration_AddBehavior_AddsToCollection()
    {
        // Arrange
        var config = new MediatorConfiguration();

        // Act
        config.AddBehavior<TestBehavior>();

        // Assert
        Assert.NotEmpty(config.BehaviorsToRegister);
    }

    [Fact]
    public void AddAllGenericImplementations_WithFilterRegistersOnlyMatchingImplementations()
    {
        // Arrange
        var services = new ServiceCollection();
        bool Filter(Type type) => type == typeof(TestImplementation2);

        // Act
        services.AddAllGenericImplementations(
            typeof(ITestInterface<>),
            ServiceLifetime.Singleton,
            Filter,
            new[] { typeof(TestImplementation).Assembly });

        // Assert
        var descriptor1 = services.FirstOrDefault(d => d.ServiceType == typeof(ITestInterface<string>));
        var descriptor2 = services.FirstOrDefault(d => d.ServiceType == typeof(ITestInterface<int>));

        Assert.Null(descriptor1); // TestImplementation non deve essere registrato
        Assert.NotNull(descriptor2); // Solo TestImplementation2 deve essere registrato
        Assert.Equal(typeof(TestImplementation2), descriptor2.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor2.Lifetime);
    }

    [Fact]
    public void AddAllGenericImplementations_EmptyAssemblies_RegistersNothing()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;

        // Act
        services.AddAllGenericImplementations(typeof(ITestInterface<>), assemblies: Array.Empty<System.Reflection.Assembly>());

        // Assert
        Assert.Equal(initialCount, services.Count);
    }

    [Fact]
    public void AddAllGenericImplementations_WithLifetimes_RegistersWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAllGenericImplementations(typeof(ITestInterface<>), ServiceLifetime.Transient, null, typeof(TestImplementation).Assembly);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITestInterface<string>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddRequestMediator_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        bool configApplied = false;

        // Act
        services.AddRequestMediator(config =>
        {
            config.AddBehavior<TestBehavior>();
            configApplied = true;
        });

        // Assert
        Assert.True(configApplied);
        Assert.Contains(services, sd => sd.ServiceType == typeof(IPipelineBehavior<TestRequest, TestResponse>));
    }

    [Fact]
    public void MessageMediatorConfiguration_AddMultipleBehaviors_MaintainsOrder()
    {
        // Arrange
        var config = new MediatorConfiguration();

        // Act
        config.AddBehavior<TestBehavior>();
        config.AddBehavior<TestBehavior2>();

        // Assert
        Assert.Equal(2, config.BehaviorsToRegister.Count);
        Assert.Equal(typeof(TestBehavior), config.BehaviorsToRegister[0].ImplementationType);
        Assert.Equal(typeof(TestBehavior2), config.BehaviorsToRegister[1].ImplementationType);
    }

    [Fact]
    public void AddRequestMediator_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddRequestMediator(null));

        Assert.Equal("configuration", exception.ParamName);
    }

    [Fact]
    public void AddRequestMediator_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Forma.Mediator.Extensions.DepenencyInjection.AddRequestMediator(null, null));

        Assert.Equal("services", exception.ParamName);
    }
}

// Helper classes
public class TestBehavior : IPipelineBehavior<TestRequest, TestResponse>
{
    public Task<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken, Func<CancellationToken, Task<TestResponse>> next)
    {
        return next(cancellationToken);
    }
}

public class TestBehavior2 : IPipelineBehavior<TestRequest, TestResponse>
{
    public Task<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken, Func<CancellationToken, Task<TestResponse>> next)
    {
        return next(cancellationToken);
    }
}
