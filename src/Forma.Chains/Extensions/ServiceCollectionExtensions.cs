using System.Reflection;
using Forma.Chains.Abstractions;
using Forma.Chains.Configurations;
using Forma.Chains.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Forma.Chains.Extensions;

/// <summary>
/// Estensioni per <see cref="IServiceCollection"/> che permettono la registrazione di catene di responsabilità.
/// </summary>
public static class ServiceCollectionExtensions
{
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
    private static IServiceCollection AddAllGenericImplementations(
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
    /// Metodo privato di supporto per registrare gli handler nella catena.
    /// </summary>
    private static IServiceCollection RegisterHandlers(
        this IServiceCollection services,
        Type[]? handlerTypes,
        ChainConfiguration? configuration = null)
    {
        configuration ??= new ChainConfiguration();
        var assemblies = configuration.Assemblies;
        var typeFilter = configuration.HandlerTypeFilter;
        
        if (handlerTypes == null || handlerTypes.Length == 0)
        {
            services.AddAllGenericImplementations(
                typeof(IChainHandler<>),
                ServiceLifetime.Transient,
                (t) => (typeFilter == null || typeFilter(t)) && 
                    t.BaseType != null &&
                    t.BaseType.IsGenericType,
                assemblies);
        }
        else
        {
            // Applica il filtro se specificato
            if (typeFilter != null)
            {
                handlerTypes = handlerTypes.Where(typeFilter).ToArray();
            }
            
            // Registra tutti i tipi di handler come servizi se non sono già registrati
            foreach (var handlerType in handlerTypes)
            {
                services.TryAddTransient(handlerType);
            }
        }

        return services;
    }

    /// <summary>
    /// Metodo privato di supporto per registrare una catena senza risposta.
    /// </summary>
    private static IServiceCollection RegisterChain<TRequest>(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        Type[]? handlerTypes,
        ChainConfiguration? configuration = null)
        where TRequest : notnull
    {
        configuration ??= new ChainConfiguration();
        
        // Ordina i tipi di handler se necessario
        if (handlerTypes != null && configuration.OrderStrategy != ChainOrderStrategy.AsProvided)
        {
            handlerTypes = ChainHelpers.OrderHandlerTypes(handlerTypes, configuration.OrderStrategy).ToArray();
        }

        // Registra gli handler
        services.RegisterHandlers(handlerTypes, configuration);

        // Registra il ChainBuilder
        var builderService = ServiceDescriptor.Describe(
            typeof(IChainBuilder<TRequest>),
            sp => new ChainBuilder<TRequest>(
                sp, 
                handlerTypes ?? [], 
                configuration),
            configuration.ChainBuilderLifetime);
            
        services.Add(builderService);

        // Registra il primo handler della catena costruito dal ChainBuilder
        var handlerService = ServiceDescriptor.Describe(
            typeof(IChainHandler<TRequest>),
            sp => sp.GetRequiredService<IChainBuilder<TRequest>>().Build(),
            lifetime);
            
        services.Add(handlerService);

        return services;
    }

    /// <summary>
    /// Metodo privato di supporto per registrare una catena con risposta.
    /// </summary>
    private static IServiceCollection RegisterChainWithResponse<TRequest, TResponse>(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        Type[]? handlerTypes,
        ChainConfiguration? configuration = null)
        where TRequest : notnull
    {
        configuration ??= new ChainConfiguration();
        
        // Registra gli handler
        services.RegisterHandlers(handlerTypes, configuration);

        // Ordina i tipi di handler se necessario
        if (handlerTypes != null && configuration.OrderStrategy != ChainOrderStrategy.AsProvided)
        {
            handlerTypes = ChainHelpers.OrderHandlerTypes(handlerTypes, configuration.OrderStrategy).ToArray();
        }

        // Registra il ChainBuilder
        var builderService = ServiceDescriptor.Describe(
            typeof(IChainBuilder<TRequest, TResponse>),
            sp => new ChainBuilder<TRequest, TResponse>(
                sp, 
                handlerTypes ?? [], 
                configuration),
            configuration.ChainBuilderLifetime);
            
        services.Add(builderService);

        // Registra il primo handler della catena costruito dal ChainBuilder
        var handlerService = ServiceDescriptor.Describe(
            typeof(IChainHandler<TRequest, TResponse>),
            sp => sp.GetRequiredService<IChainBuilder<TRequest, TResponse>>().Build(),
            lifetime);
            
        services.Add(handlerService);

        return services;
    }

    /// <summary>
    /// Aggiunge una catena di responsabilità al container di dipendenze.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
    /// <param name="services">La collezione di servizi.</param>
    /// <param name="configureOptions">Configurazione della catena.</param>
    /// <returns>La collezione di servizi aggiornata.</returns>
    public static IServiceCollection AddChain<TRequest>(
        this IServiceCollection services,
        Action<ChainConfiguration> configureOptions)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        
        var configuration = new ChainConfiguration();
        configureOptions(configuration);
        
        return services.RegisterChain<TRequest>(ServiceLifetime.Transient, [], configuration);
    }

    /// <summary>
    /// Aggiunge una catena di responsabilità al container di dipendenze.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
    /// <param name="services">La collezione di servizi.</param>
    /// <param name="configureOptions">Configurazione della catena.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena, nell'ordine corretto.</param>
    /// <returns>La collezione di servizi aggiornata.</returns>
    public static IServiceCollection AddChain<TRequest>(
        this IServiceCollection services,
        Action<ChainConfiguration> configureOptions,
        params Type[] handlerTypes)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        
        var configuration = new ChainConfiguration();
        configureOptions(configuration);
        
        return services.RegisterChain<TRequest>(ServiceLifetime.Transient, handlerTypes, configuration);
    }

    /// <summary>
    /// Aggiunge una catena di responsabilità al container di dipendenze.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
    /// <param name="services">La collezione di servizi.</param>
    /// <returns>La collezione di servizi aggiornata.</returns>
    public static IServiceCollection AddChain<TRequest>(
        this IServiceCollection services)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.RegisterChain<TRequest>(ServiceLifetime.Transient, [], new ChainConfiguration());
    }

    /// <summary>
    /// Aggiunge una catena di responsabilità al container di dipendenze.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
    /// <param name="services">La collezione di servizi.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena, nell'ordine corretto.</param>
    /// <returns>La collezione di servizi aggiornata.</returns>
    public static IServiceCollection AddChain<TRequest>(
        this IServiceCollection services,
        params Type[] handlerTypes)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.RegisterChain<TRequest>(ServiceLifetime.Transient, handlerTypes, new ChainConfiguration());
    }

    /// <summary>
    /// Aggiunge una catena di responsabilità al container di dipendenze.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
    /// <param name="services">La collezione di servizi.</param>
    /// <param name="lifetime">La durata del servizio.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena, nell'ordine corretto.</param>
    /// <returns>La collezione di servizi aggiornata.</returns>
    public static IServiceCollection AddChain<TRequest>(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        params Type[] handlerTypes)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.RegisterChain<TRequest>(lifetime, handlerTypes, new ChainConfiguration());
    }

    /// <summary>
    /// Aggiunge una catena di responsabilità al container di dipendenze.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
    /// <param name="services">La collezione di servizi.</param>
    /// <param name="lifetime">La durata del servizio.</param>
    /// <param name="configureOptions">Configurazione della catena.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena, nell'ordine corretto.</param>
    /// <returns>La collezione di servizi aggiornata.</returns>
    public static IServiceCollection AddChain<TRequest>(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        Action<ChainConfiguration> configureOptions,
        params Type[] handlerTypes)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        
        var configuration = new ChainConfiguration();
        configureOptions(configuration);
        
        return services.RegisterChain<TRequest>(lifetime, handlerTypes, configuration);
    }

    /// <summary>
    /// Aggiunge una catena di responsabilità che restituisce una risposta al container di dipendenze.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta che viene restituita dalla catena.</typeparam>
    /// <param name="services">La collezione di servizi.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena, nell'ordine corretto.</param>
    /// <returns>La collezione di servizi aggiornata.</returns>
    public static IServiceCollection AddChain<TRequest, TResponse>(
        this IServiceCollection services,
        params Type[] handlerTypes)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.RegisterChainWithResponse<TRequest, TResponse>(ServiceLifetime.Transient, handlerTypes, new ChainConfiguration());
    }

    /// <summary>
    /// Aggiunge una catena di responsabilità che restituisce una risposta al container di dipendenze.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta che viene restituita dalla catena.</typeparam>
    /// <param name="services">La collezione di servizi.</param>
    /// <param name="configureOptions">Configurazione della catena.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena, nell'ordine corretto.</param>
    /// <returns>La collezione di servizi aggiornata.</returns>
    public static IServiceCollection AddChain<TRequest, TResponse>(
        this IServiceCollection services,
        Action<ChainConfiguration> configureOptions,
        params Type[] handlerTypes)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        
        var configuration = new ChainConfiguration();
        configureOptions(configuration);
        
        return services.RegisterChainWithResponse<TRequest, TResponse>(ServiceLifetime.Transient, handlerTypes, configuration);
    }

    /// <summary>
    /// Aggiunge una catena di responsabilità che restituisce una risposta al container di dipendenze.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta che viene restituita dalla catena.</typeparam>
    /// <param name="services">La collezione di servizi.</param>
    /// <param name="lifetime">La durata del servizio.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena, nell'ordine corretto.</param>
    /// <returns>La collezione di servizi aggiornata.</returns>
    public static IServiceCollection AddChain<TRequest, TResponse>(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        params Type[] handlerTypes)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.RegisterChainWithResponse<TRequest, TResponse>(lifetime, handlerTypes, new ChainConfiguration());
    }

    /// <summary>
    /// Aggiunge una catena di responsabilità che restituisce una risposta al container di dipendenze.
    /// </summary>
    /// <typeparam name="TRequest">Il tipo di richiesta che viene gestita dalla catena.</typeparam>
    /// <typeparam name="TResponse">Il tipo di risposta che viene restituita dalla catena.</typeparam>
    /// <param name="services">La collezione di servizi.</param>
    /// <param name="lifetime">La durata del servizio.</param>
    /// <param name="configureOptions">Configurazione della catena.</param>
    /// <param name="handlerTypes">I tipi di handler da includere nella catena, nell'ordine corretto.</param>
    /// <returns>La collezione di servizi aggiornata.</returns>
    public static IServiceCollection AddChain<TRequest, TResponse>(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        Action<ChainConfiguration> configureOptions,
        params Type[] handlerTypes)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        
        var configuration = new ChainConfiguration();
        configureOptions(configuration);
        
        return services.RegisterChainWithResponse<TRequest, TResponse>(lifetime, handlerTypes, configuration);
    }
}
