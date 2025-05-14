using Microsoft.Extensions.DependencyInjection;

namespace Forma.Decorator.Extensions;

/// <summary>
/// Estension per IServiceCollection per decorare i servizi
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Decorate un servizio con uno o più decoratori
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <param name="services"></param>
    /// <param name="decorators"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IServiceCollection Decorate<TService>(
    this IServiceCollection services,
    params Type[] decorators)
    where TService : class
    {
        if (decorators == null || decorators.Length == 0)
            throw new ArgumentException("Almeno un decoratore è richiesto.");

        foreach (var decorator in decorators)
        {
            services.Decorate(typeof(TService), decorator);
        }

        return services;
    }

    /// <summary>
    /// Controlla se il decoratore è valido per il tipo di servizio
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="decoratorType"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void EnsureValidDecorator(Type serviceType, Type decoratorType)
    {
        var ctor = decoratorType
            .GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Any(p => serviceType.IsAssignableFrom(p.ParameterType)));

        if (ctor == null)
        {
            throw new InvalidOperationException(
                $"Il decoratore {decoratorType.Name} deve avere un costruttore con almeno un parametro assegnabile a {serviceType.Name}.");
        }
    }

    /// <summary>
    /// Decorate un servizio con un decoratore specifico
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceType"></param>
    /// <param name="decoratorType"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IServiceCollection Decorate(
        this IServiceCollection services,
        Type serviceType,
        Type decoratorType)
    {
        EnsureValidDecorator(serviceType, decoratorType);

        // Trova l'ultimo descriptor registrato per il tipo di servizio
        var descriptor = services.LastOrDefault(d => d.ServiceType == serviceType);
        if (descriptor == null)
            throw new InvalidOperationException($"Servizio {serviceType} non registrato.");

        // Rimuovi la registrazione originale
        services.Remove(descriptor);

        // Genera una chiave univoca per il servizio originale
        var serviceKey = Guid.NewGuid();

        // Registra il servizio originale con la chiave
        if (descriptor.ImplementationInstance != null)
        {
            // Per istanze singleton
            services.Add(ServiceDescriptor.KeyedSingleton(
                serviceType,
                serviceKey,
                descriptor.ImplementationInstance));
        }
        else if (descriptor.ImplementationType != null)
        {
            // Per tipi concreti
            services.Add(ServiceDescriptor.DescribeKeyed(
                serviceType,
                serviceKey,
                descriptor.ImplementationType,
                descriptor.Lifetime));
        }
        else if (descriptor.ImplementationFactory != null)
        {
            // Per factory esistenti
            services.Add(new ServiceDescriptor(
                serviceType,
                serviceKey,
                (sp, _) => descriptor.ImplementationFactory(sp),
                descriptor.Lifetime));
        }

        // Registra il decoratore che usa il servizio originale con chiave
        services.Add(ServiceDescriptor.Describe(
            serviceType,
            sp =>
            {
                // Ottieni l'istanza del servizio originale tramite la chiave
                var inner = sp.GetRequiredKeyedService(serviceType, serviceKey);

                // Crea il decoratore passandogli l'istanza del servizio originale
                return ActivatorUtilities.CreateInstance(sp, decoratorType, inner);
            },
            descriptor.Lifetime));

        return services;
    }

    /// <summary>
    /// Decorate un servizio con un decoratore specifico
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TDecorator"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection Decorate<TService, TDecorator>(
        this IServiceCollection services)
        where TService : class
        where TDecorator : class, TService
    {
        return services.Decorate(typeof(TService), typeof(TDecorator));
    }
}