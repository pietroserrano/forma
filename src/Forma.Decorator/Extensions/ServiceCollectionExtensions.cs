using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Forma.Decorator.Extensions;

/// <summary>
/// Estension per IServiceCollection per decorare i servizi
/// </summary>
public static class ServiceCollectionExtensions
{
    // Cache per i costruttori dei decoratori
    private static readonly ConcurrentDictionary<(Type service, Type decorator), (ConstructorInfo ctor, int paramIndex)> _ctorCache = 
        new ConcurrentDictionary<(Type service, Type decorator), (ConstructorInfo ctor, int paramIndex)>();
    
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

        Type serviceType = typeof(TService);
        for (int i = 0; i < decorators.Length; i++)
        {
            services.Decorate(serviceType, decorators[i]);
        }

        return services;
    }

    /// <summary>
    /// Controlla se il decoratore è valido per il tipo di servizio e restituisce le informazioni sul costruttore
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="decoratorType"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static (ConstructorInfo ctor, int paramIndex) EnsureValidDecorator(Type serviceType, Type decoratorType)
    {
        // Verifica se la combinazione è già presente nella cache
        if (_ctorCache.TryGetValue((serviceType, decoratorType), out var cached))
            return cached;
            
        // Cerca il costruttore adatto
        var constructors = decoratorType.GetConstructors();
        for (int i = 0; i < constructors.Length; i++)
        {
            var ctor = constructors[i];
            var parameters = ctor.GetParameters();
            
            for (int j = 0; j < parameters.Length; j++)
            {
                if (serviceType.IsAssignableFrom(parameters[j].ParameterType))
                {
                    var result = (ctor, j);
                    // Memorizza nella cache
                    _ctorCache[(serviceType, decoratorType)] = result;
                    return result;
                }
            }
        }

        throw new InvalidOperationException(
            $"Il decoratore {decoratorType.Name} deve avere un costruttore con almeno un parametro assegnabile a {serviceType.Name}.");
    }    /// <summary>
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
        // Ottieni le informazioni sul costruttore e sull'indice del parametro
        var (ctor, paramIndex) = EnsureValidDecorator(serviceType, decoratorType);
        
        // Trova l'ultimo descriptor registrato per il tipo di servizio
        // Ottimizzazione: cerca dall'ultimo elemento in poi per migliorare la performance
        ServiceDescriptor? descriptor = null;
        for (int i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == serviceType)
            {
                descriptor = services[i];
                break;
            }
        }

        if (descriptor == null)
            throw new InvalidOperationException($"Servizio {serviceType} non registrato.");

        // Rimuovi la registrazione originale
        services.Remove(descriptor);

        // Genera una chiave univoca per il servizio originale (più efficiente di Guid.NewGuid)
        var serviceKey = RuntimeHelpers.GetHashCode(descriptor);

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

                // return ActivatorUtilities.CreateInstance(sp, decoratorType, inner);                
                // Factory ottimizzata per la creazione dell'istanza del decoratore
                var ctorParams = ctor.GetParameters();
                var parameters = new object[ctorParams.Length];
                parameters[paramIndex] = inner;
                  // Ottieni gli altri parametri dal container se necessario
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i == paramIndex) continue;
                    var paramType = ctorParams[i].ParameterType;
                    var service = sp.GetService(paramType);
                    if (service != null)
                        parameters[i] = service;
                    else if (ctorParams[i].HasDefaultValue)
                        parameters[i] = ctorParams[i].DefaultValue!;
                    else if (paramType.IsValueType)
                        parameters[i] = Activator.CreateInstance(paramType)!;
                }
                
                // Crea direttamente l'istanza usando Invoke che è più veloce di ActivatorUtilities
                return ctor.Invoke(parameters);
            },
            descriptor.Lifetime));

        return services;
    }    /// <summary>
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
        // Chiamata diretta senza usare typeof per migliorare le prestazioni
        Type serviceType = typeof(TService);
        Type decoratorType = typeof(TDecorator);
        return services.Decorate(serviceType, decoratorType);
    }
}