using Forma.Abstractions;
using Forma.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Tests.Mediator;

public class MessageMediatorConfigurationTests
{
    // Comportamento generico per test open generic
    public class GenericBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<CancellationToken, Task<TResponse>> next)
            => next(cancellationToken);
    }

    // Comportamento non valido (non generico)
    public class InvalidBehavior { }

    public abstract class AbstractBehavior : IPipelineBehavior<TestRequest, TestResponse>
    {
        public abstract Task<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken, Func<CancellationToken, Task<TestResponse>> next);
    }

    public abstract class AbstractGenericBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public abstract Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<CancellationToken, Task<TResponse>> next);
    }

    [Fact]
    public void AddBehavior_WithClosedType_AddsDescriptor()
    {
        var config = new MediatorConfiguration();

        config.AddBehavior<TestBehavior>();

        Assert.Single(config.BehaviorsToRegister);
        var descriptor = config.BehaviorsToRegister.First();
        Assert.Equal(typeof(IPipelineBehavior<TestRequest, TestResponse>), descriptor.ServiceType);
        Assert.Equal(typeof(TestBehavior), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddBehavior_WithTypeNotImplementingInterface_Throws()
    {
        var config = new MediatorConfiguration();

        Assert.Throws<InvalidOperationException>(() =>
            config.AddBehavior(typeof(InvalidBehavior)));
    }

    [Fact]
    public void AddOpenBehavior_WithOpenGeneric_AddsDescriptor()
    {
        var config = new MediatorConfiguration();

        config.AddOpenBehavior(typeof(GenericBehavior<,>), ServiceLifetime.Scoped);

        Assert.Single(config.BehaviorsToRegister);
        var descriptor = config.BehaviorsToRegister.First();
        Assert.Equal(typeof(IPipelineBehavior<,>), descriptor.ServiceType);
        Assert.Equal(typeof(GenericBehavior<,>), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddOpenBehavior_WithNonGenericType_Throws()
    {
        var config = new MediatorConfiguration();

        Assert.Throws<InvalidOperationException>(() =>
            config.AddOpenBehavior(typeof(TestBehavior)));
    }

    [Fact]
    public void AddOpenBehavior_WithTypeNotImplementingInterface_Throws()
    {
        var config = new MediatorConfiguration();

        Assert.Throws<InvalidOperationException>(() =>
            config.AddOpenBehavior(typeof(InvalidBehavior)));
    }
    [Fact]
    public void AddBehavior_WithNullType_Throws()
    {
        var config = new MediatorConfiguration();
        Assert.Throws<ArgumentNullException>(() => config.AddBehavior(null!));
    }

    [Fact]
    public void AddOpenBehavior_WithNullType_Throws()
    {
        var config = new MediatorConfiguration();
        Assert.Throws<ArgumentNullException>(() => config.AddOpenBehavior(null!));
    }

    [Fact]
    public void AddOpenBehavior_WithNullLifetime_UsesDefault()
    {
        var config = new MediatorConfiguration();
        config.AddOpenBehavior(typeof(GenericBehavior<,>));
        var descriptor = config.BehaviorsToRegister.First();
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddBehavior_WithDuplicateType_AddsMultipleDescriptors()
    {
        var config = new MediatorConfiguration();
        config.AddBehavior<TestBehavior>();
        config.AddBehavior<TestBehavior>();
        Assert.Equal(2, config.BehaviorsToRegister.Count());
    }

    [Fact]
    public void AddOpenBehavior_WithClosedGenericType_Throws()
    {
        var config = new MediatorConfiguration();
        Assert.Throws<InvalidOperationException>(() =>
            config.AddOpenBehavior(typeof(GenericBehavior<TestRequest, TestResponse>)));
    }

    [Fact]
    public void AddBehavior_WithAbstractType_Throws()
    {
        var config = new MediatorConfiguration();
        Assert.Throws<InvalidOperationException>(() =>
            config.AddBehavior(typeof(AbstractBehavior)));
    }

    [Fact]
    public void AddOpenBehavior_WithAbstractGenericType_Throws()
    {
        var config = new MediatorConfiguration();
        Assert.Throws<InvalidOperationException>(() =>
            config.AddOpenBehavior(typeof(AbstractGenericBehavior<,>)));
    }

}
