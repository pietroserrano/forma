using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Mediator.Behaviors;
using Forma.Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Forma.Mediator.Extensions;

/// <summary>
/// Extension methods for adding Forma Mediator services to the dependency injection container.
/// </summary>
public static class DepenencyInjection
{
    /// <summary>
    /// Adds the Forma Mediator services to the dependency injection container.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddRequestMediator(this IServiceCollection services, Action<MediatorConfiguration> configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var serviceConfig = new MediatorConfiguration();

        configuration.Invoke(serviceConfig);

        var assemblies = serviceConfig.AssembliesToRegister.Distinct().ToArray();

        services.AddAllGenericImplementations(typeof(IHandler<>), ServiceLifetime.Scoped, null, assemblies);
        services.AddAllGenericImplementations(typeof(IHandler<,>), ServiceLifetime.Scoped, null, assemblies);

        AddRequiredServices(services, serviceConfig);

        return services;
    }

    /// <summary>
    /// Adds all implementations of a generic interface type to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="genericInterfaceType"></param>
    /// <param name="lifetime"></param>
    /// <param name="typeFilter">Opzionale: filtro per il tipo di implementazione</param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IServiceCollection AddAllGenericImplementations(
            this IServiceCollection services,
            Type genericInterfaceType,
            ServiceLifetime lifetime = ServiceLifetime.Scoped,
            Func<Type, bool>? typeFilter = null,
            params Assembly[] assemblies)
    {
        if (!genericInterfaceType.IsInterface)
            throw new ArgumentException("Deve essere un'interfaccia", nameof(genericInterfaceType));

        bool isOpenGeneric = genericInterfaceType.IsGenericTypeDefinition;

        if (assemblies == null || assemblies.Length == 0)
            return services;

        var types = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var type in types)
        {
            if (typeFilter != null && !typeFilter(type))
                continue;

            var interfaces = type.GetInterfaces()
                .Where(i =>
                    i.IsGenericType &&
                    (isOpenGeneric
                        ? i.GetGenericTypeDefinition() == genericInterfaceType
                        : i == genericInterfaceType));

            foreach (var @interface in interfaces)
            {
                services.Add(new ServiceDescriptor(@interface, type, lifetime));
            }
        }

        return services;
    }

    /// <summary>
    /// Adds the required services for the Forma Mediator to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceConfiguration"></param>
    public static void AddRequiredServices(IServiceCollection services, MediatorConfiguration serviceConfiguration)
    {
        services.TryAdd(new ServiceDescriptor(typeof(RequestHandlerImpl<,>), typeof(RequestHandlerImpl<,>), serviceConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(RequestHandlerImpl<>), typeof(RequestHandlerImpl<>), serviceConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IRequestMediator), typeof(RequestMediator), serviceConfiguration.Lifetime));

        if (serviceConfiguration.MessagePreProcessorsToRegister.Any())
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>), ServiceLifetime.Transient));
            services.TryAddEnumerable(serviceConfiguration.MessagePreProcessorsToRegister);
        }

        if (serviceConfiguration.MessagePostProcessorsToRegister.Any())
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>), ServiceLifetime.Transient));
            services.TryAddEnumerable(serviceConfiguration.MessagePostProcessorsToRegister);
        }

        foreach (var serviceDescriptor in serviceConfiguration.BehaviorsToRegister)
        {
            services.TryAddEnumerable(serviceDescriptor);
        }
    }

    internal static IEnumerable<Type> FindInterfacesThatClose(this Type concreteType, Type templateType)
    {
        static bool IsConcrete(Type type)
        {
            return !type.IsAbstract && !type.IsInterface;
        }

        if (concreteType == null) yield break;

        if (!IsConcrete(concreteType)) yield break;

        if (templateType.IsInterface)
        {
            foreach (
                var interfaceType in
                concreteType.GetInterfaces()
                    .Where(type => type.IsGenericType && (type.GetGenericTypeDefinition() == templateType)))
            {
                yield return interfaceType;
            }
        }
        else if (concreteType.BaseType!.IsGenericType &&
                 (concreteType.BaseType!.GetGenericTypeDefinition() == templateType))
        {
            yield return concreteType.BaseType!;
        }

        if (concreteType.BaseType == typeof(object)) yield break;

        foreach (var interfaceType in FindInterfacesThatClose(concreteType.BaseType!, templateType))
        {
            yield return interfaceType;
        }
    }
}

/// <summary>
/// Configuration class for the Forma Mediator.
/// </summary>
public class MediatorConfiguration()
{
    /// <summary>
    /// The lifetime of the services to be registered.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
    /// <summary>
    /// The type of the notification publisher to be registered.
    /// </summary>
    public List<ServiceDescriptor> BehaviorsToRegister { get; } = new();
    /// <summary>
    /// The type of the notification publisher to be registered.
    /// </summary>
    public List<ServiceDescriptor> MessagePreProcessorsToRegister { get; } = new();
    /// <summary>
    /// The type of the notification publisher to be registered.
    /// </summary>
    public List<ServiceDescriptor> MessagePostProcessorsToRegister { get; } = new();

    internal List<Assembly> AssembliesToRegister { get; } = new();

    /// <summary>
    /// Register various handlers from assembly containing given type
    /// </summary>
    /// <typeparam name="T">Type from assembly to scan</typeparam>
    /// <returns>This</returns>
    public MediatorConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssemblyContaining(typeof(T));

    /// <summary>
    /// Register various handlers from assembly containing given type
    /// </summary>
    /// <param name="type">Type from assembly to scan</param>
    /// <returns>This</returns>
    public MediatorConfiguration RegisterServicesFromAssemblyContaining(Type type)
        => RegisterServicesFromAssembly(type.Assembly);

    /// <summary>
    /// Register a closed behavior type
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public MediatorConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        AssembliesToRegister.Add(assembly);

        return this;
    }

    /// <summary>
    /// Register a closed behavior type
    /// </summary>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public MediatorConfiguration RegisterServicesFromAssemblies(
    params Assembly[] assemblies)
    {
        AssembliesToRegister.AddRange(assemblies);

        return this;
    }

    /// <summary>
    /// Register a closed behavior type
    /// </summary>
    /// <typeparam name="TServiceType">Closed behavior interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed behavior implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddBehavior<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddBehavior(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed behavior type against all <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed behavior implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddBehavior<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        return AddBehavior(typeof(TImplementationType), serviceLifetime);
    }

    /// <summary>
    /// Register a closed behavior type against all <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed behavior implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddBehavior(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(implementationType);

        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IPipelineBehavior<,>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IPipelineBehavior<,>).FullName}");
        }

        foreach (var implementedBehaviorType in implementedGenericInterfaces)
        {
            BehaviorsToRegister.Add(new ServiceDescriptor(implementedBehaviorType, implementationType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Register a closed behavior type
    /// </summary>
    /// <param name="serviceType">Closed behavior interface type</param>
    /// <param name="implementationType">Closed behavior implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddBehavior(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        BehaviorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Registers an open behavior type against the <see cref="IPipelineBehavior{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic behavior type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddOpenBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);

        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        if (!openBehaviorType.IsGenericTypeDefinition)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be an open generic type");
        }

        if (openBehaviorType.IsAbstract)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be an abstract type");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IPipelineBehavior<,>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IPipelineBehavior<,>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            BehaviorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Registers multiple open behavior types against the <see cref="IPipelineBehavior{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorTypes">An open generic behavior type list includes multiple open generic behavior types.</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddOpenBehaviors(IEnumerable<Type> openBehaviorTypes, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        foreach (var openBehaviorType in openBehaviorTypes)
        {
            AddOpenBehavior(openBehaviorType, serviceLifetime);
        }

        return this;
    }

    /// <summary>
    /// Register a closed request pre processor type
    /// </summary>
    /// <typeparam name="TServiceType">Closed request pre processor interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed request pre processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddRequestPreProcessor<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPreProcessor(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request pre processor type
    /// </summary>
    /// <param name="serviceType">Closed request pre processor interface type</param>
    /// <param name="implementationType">Closed request pre processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddRequestPreProcessor(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        MessagePreProcessorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Register a closed request pre processor type against all <see cref="IRequestPreProcessor{TMessage}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed request pre processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddRequestPreProcessor<TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPreProcessor(typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request pre processor type against all <see cref="IRequestPreProcessor{TMessage}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed request pre processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddRequestPreProcessor(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IRequestPreProcessor<>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IRequestPreProcessor<>).FullName}");
        }

        foreach (var implementedPreProcessorType in implementedGenericInterfaces)
        {
            MessagePreProcessorsToRegister.Add(new ServiceDescriptor(implementedPreProcessorType, implementationType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Registers an open request pre processor type against the <see cref="IRequestPreProcessor{TRequest}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic request pre processor type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddOpenRequestPreProcessor(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IRequestPreProcessor<>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IRequestPreProcessor<>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            MessagePreProcessorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Register a closed request post processor type
    /// </summary>
    /// <typeparam name="TServiceType">Closed request post processor interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed request post processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddRequestPostProcessor<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPostProcessor(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request post processor type
    /// </summary>
    /// <param name="serviceType">Closed request post processor interface type</param>
    /// <param name="implementationType">Closed request post processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddRequestPostProcessor(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        MessagePostProcessorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Register a closed request post processor type against all <see cref="IRequestPostProcessor{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed request post processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddRequestPostProcessor<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPostProcessor(typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request post processor type against all <see cref="IRequestPostProcessor{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed request post processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddRequestPostProcessor(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IRequestPostProcessor<,>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IRequestPostProcessor<,>).FullName}");
        }

        foreach (var implementedPostProcessorType in implementedGenericInterfaces)
        {
            MessagePostProcessorsToRegister.Add(new ServiceDescriptor(implementedPostProcessorType, implementationType, serviceLifetime));
        }
        return this;
    }

    /// <summary>
    /// Registers an open request post processor type against the <see cref="IRequestPostProcessor{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic request post processor type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatorConfiguration AddOpenRequestPostProcessor(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IRequestPostProcessor<,>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IRequestPostProcessor<,>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            MessagePostProcessorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }
}
