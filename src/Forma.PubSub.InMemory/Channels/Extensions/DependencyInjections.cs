using System.Reflection;
using Forma.Core.PubSub.Abstractions;
using Forma.PubSub.InMemory.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.PubSub.InMemory.ChannelPubSub.Extensions;

/// <summary>
/// This class contains extension methods for setting up Forma PubSub InMemory channels.
/// </summary>
public static class DependencyInjections
{
    /// <summary>
    /// Adds Forma PubSub InMemory channels to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembliesToScan">Assemblies to scan for implementations of IConsume&lt;TMessage&gt;.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddFormaPubSubInMemory(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        AddAllGenericImplementations(
            services,
            typeof(IConsume<>),
            ServiceLifetime.Scoped,
            typeFilter: t => t.IsClass && !t.IsAbstract,
            assembliesToScan);

        services.AddTransient<IBus, InMemoryBusImpl>();
        
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
}
