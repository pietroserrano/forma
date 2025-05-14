using Forma.Decorator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Tests.Decorator;

public class DecorateFixtures
{        
    // Test base: verifica che il decoratore venga applicato correttamente
    [Fact]
    public void DecorateWithKeyed_AppliesDecorator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMyService, MyService>();

        // Act
        services.Decorate<IMyService, TestDecorator>();
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IMyService>();

        // Assert
        Assert.IsType<TestDecorator>(service);
        Assert.IsType<MyService>((service as TestDecorator).Inner);
    }

    // Test che verifica la catena di decorazione
    [Fact]
    public void DecorateWithKeyed_MultipleDecorators_AppliesInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMyService, MyService>();

        // Act - con l'implementazione attuale, l'ultimo decoratore applicato diventa quello più esterno
        // Quindi per ottenere OuterDecorator -> InnerDecorator -> MyService, applichiamo prima InnerDecorator e poi OuterDecorator
        services.Decorate<IMyService, InnerDecorator>();
        services.Decorate<IMyService, OuterDecorator>();
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IMyService>();

        // Assert - verifica che OuterDecorator contenga InnerDecorator che contenga MyService
        Assert.IsType<OuterDecorator>(service);
        var outerDecorator = service as OuterDecorator;
        Assert.IsType<InnerDecorator>(outerDecorator.Inner);
        var innerDecorator = outerDecorator.Inner as InnerDecorator;
        Assert.IsType<MyService>(innerDecorator.Inner);
    }

    // Test con registrazione singleton
    [Fact]
    public void DecorateWithKeyed_WithSingleton_MaintainsLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMyService>(new MyService());

        // Act
        services.Decorate<IMyService, TestDecorator>();
        var provider = services.BuildServiceProvider();

        // Request the service twice to verify it's the same instance
        var service1 = provider.GetRequiredService<IMyService>();
        var service2 = provider.GetRequiredService<IMyService>();

        // Assert
        Assert.Same(service1, service2); // Verifica che sia lo stesso oggetto (singleton)
    }

    // Test con registrazione tramite factory
    [Fact]
    public void DecorateWithKeyed_WithFactory_AppliesDecorator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMyService>(sp => new MyService { Name = "Factory-Created" });

        // Act
        services.Decorate<IMyService, TestDecorator>();
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IMyService>();

        // Assert
        Assert.IsType<TestDecorator>(service);
        var testDecorator = service as TestDecorator;
        Assert.IsType<MyService>(testDecorator.Inner);
        Assert.Equal("Factory-Created", (testDecorator.Inner as MyService).Name);
    }

    // Test con registrazione di istanza
    [Fact]
    public void DecorateWithKeyed_WithInstance_AppliesDecorator()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new MyService { Name = "Singleton-Instance" };
        services.AddSingleton<IMyService>(instance);

        // Act
        services.Decorate<IMyService, TestDecorator>();
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IMyService>();

        // Assert
        Assert.IsType<TestDecorator>(service);
        var testDecorator = service as TestDecorator;
        Assert.Same(instance, testDecorator.Inner);
    }

    // Test con classi generiche
    [Fact]
    public void DecorateWithKeyed_WithGenericService_AppliesDecorator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IGenericService<string>, GenericService<string>>();

        // Act
        services.Decorate<IGenericService<string>, GenericDecorator<string>>();
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IGenericService<string>>();

        // Assert
        Assert.IsType<GenericDecorator<string>>(service);
        var decorator = service as GenericDecorator<string>;
        Assert.IsType<GenericService<string>>(decorator.Inner);
    }

    // Test con catena di decoratori generici
    [Fact]
    public void DecorateWithKeyed_WithGenericService_AppliesMultipleDecorators()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IGenericService<int>, GenericService<int>>();

        // Act
        services.Decorate<IGenericService<int>, GenericDecorator<int>>();
        services.Decorate<IGenericService<int>, GenericLoggingDecorator<int>>();
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IGenericService<int>>();

        // Assert - verifica che la catena sia corretta
        Assert.IsType<GenericLoggingDecorator<int>>(service);
        var outerDecorator = service as GenericLoggingDecorator<int>;
        Assert.IsType<GenericDecorator<int>>(outerDecorator.Inner);
        var innerDecorator = outerDecorator.Inner as GenericDecorator<int>;
        Assert.IsType<GenericService<int>>(innerDecorator.Inner);

        // Verifica che il servizio funzioni
        int result = service.Process(10);
        Assert.Equal(10, result); // Il processo dovrebbe restituire il valore immutato
    }

    // Test con tipi generici vincolati
    [Fact]
    public void DecorateWithKeyed_WithConstrainedGenericService_AppliesDecorator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IConstrainedGenericService<TestEntity>, ConstrainedGenericService<TestEntity>>();

        // Act
        services.Decorate<IConstrainedGenericService<TestEntity>, ConstrainedGenericDecorator<TestEntity>>();
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IConstrainedGenericService<TestEntity>>();

        // Assert
        Assert.IsType<ConstrainedGenericDecorator<TestEntity>>(service);
    }

    // Test con un servizio non registrato
    [Fact]
    public void DecorateWithKeyed_NoRegisteredService_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.Decorate<IMyService, TestDecorator>());
    }
}

// Interfacce e classi per test standard
public interface IMyService
{
    void Execute();
    string GetName();
}

public class MyService : IMyService
{
    public string Name { get; set; } = "Default";

    public void Execute() { }

    public string GetName() => Name;
}

public class TestDecorator : IMyService
{
    public IMyService Inner { get; }

    public TestDecorator(IMyService inner)
    {
        Inner = inner;
    }

    public void Execute() => Inner.Execute();

    public string GetName() => $"TestDecorator({Inner.GetName()})";
}

public class InnerDecorator : IMyService
{
    public IMyService Inner { get; }

    public InnerDecorator(IMyService inner)
    {
        Inner = inner;
    }

    public void Execute() => Inner.Execute();

    public string GetName() => $"InnerDecorator({Inner.GetName()})";
}

public class OuterDecorator : IMyService
{
    public IMyService Inner { get; }

    public OuterDecorator(IMyService inner)
    {
        Inner = inner;
    }

    public void Execute() => Inner.Execute();

    public string GetName() => $"OuterDecorator({Inner.GetName()})";
}

// Interfacce e classi per test generici
public interface IGenericService<T>
{
    void Execute(T item);
    T Process(T item);
}

public class GenericService<T> : IGenericService<T>
{
    public void Execute(T item) { }

    public T Process(T item) => item;
}

public class GenericDecorator<T> : IGenericService<T>
{
    public IGenericService<T> Inner { get; }

    public GenericDecorator(IGenericService<T> inner)
    {
        Inner = inner;
    }

    public void Execute(T item) => Inner.Execute(item);

    public T Process(T item) => Inner.Process(item);
}

public class GenericLoggingDecorator<T> : IGenericService<T>
{
    public IGenericService<T> Inner { get; }

    public GenericLoggingDecorator(IGenericService<T> inner)
    {
        Inner = inner;
    }

    public void Execute(T item) => Inner.Execute(item);

    public T Process(T item) => Inner.Process(item);
}

// Classi per test generici con vincoli
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public interface IConstrainedGenericService<T> where T : class
{
    T Process(T entity);
}

public class ConstrainedGenericService<T> : IConstrainedGenericService<T> where T : class
{
    public T Process(T entity) => entity;
}

public class ConstrainedGenericDecorator<T> : IConstrainedGenericService<T> where T : class
{
    private readonly IConstrainedGenericService<T> _inner;

    public ConstrainedGenericDecorator(IConstrainedGenericService<T> inner)
    {
        _inner = inner;
    }

    public T Process(T entity) => _inner.Process(entity);
}
