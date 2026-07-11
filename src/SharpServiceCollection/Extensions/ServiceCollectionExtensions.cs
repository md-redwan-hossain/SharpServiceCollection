using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServicesFromAssemblyContaining<T>(this IServiceCollection services)
    {
        return services.AddServicesFromAssembly(typeof(T).Assembly);
    }

    public static IServiceCollection AddServicesFromAssemblyContaining(this IServiceCollection services, Type type)
    {
        return services.AddServicesFromAssembly(type.Assembly);
    }

    public static IServiceCollection AddServicesFromCurrentAssembly(this IServiceCollection services)
    {
        return services.AddServicesFromAssembly(Assembly.GetCallingAssembly());
    }

    public static IServiceCollection AddServicesFromAssembly(this IServiceCollection services,
        Assembly assembly)
    {
        MapInjectableDependencyMatchingInterface(ref services, assembly);
        MapInjectableDependencyImplementedInterface(ref services, assembly);
        MapInjectableDependencySelf(ref services, assembly);
        MapInjectableDependencyGeneric(ref services, assembly);

        return services;
    }

    private static void MapInjectableDependencyGeneric(ref IServiceCollection services, Assembly assembly)
    {
        var entries = GetSortedGenericEntries(assembly, typeof(InjectableDependencyAttribute<>));

        foreach (var (implType, attribute) in entries)
        {
            var resolverType = attribute.GetType().GetGenericArguments()[0];

            if (attribute is not (IServiceLifetime attributeWithLifetime
                and IServiceKey attributeWithServiceKey
                and ITryAddService attributeWithTryAdd))
            {
                continue;
            }

            var isKeyed = !string.IsNullOrEmpty(attributeWithServiceKey.Key);

            if (isKeyed)
            {
                RegisterKeyed(services, resolverType, implType, attributeWithLifetime.Lifetime,
                    attributeWithServiceKey.Key, attributeWithTryAdd.TryAdd);
            }
            else
            {
                RegisterNonKeyed(services, resolverType, implType, attributeWithLifetime.Lifetime,
                    attributeWithTryAdd.TryAdd, attributeWithTryAdd.Enumerable);
            }
        }
    }

    private static void MapInjectableDependencyImplementedInterface(ref IServiceCollection services,
        Assembly assembly)
    {
        var entries = GetSortedEntries(assembly, t =>
            t.GetCustomAttributes<InjectableDependencyAttribute>()
                .Where(a => a.ResolveBy == ResolveBy.ImplementedInterface));

        foreach (var (implType, attribute) in entries)
        {
            var interfaceTypes = implType.GetInterfaces();

            foreach (var interfaceType in interfaceTypes)
            {
                if (!interfaceType.IsAssignableFrom(implType))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(attribute.Key))
                {
                    RegisterKeyed(services, interfaceType, implType, attribute.Lifetime, attribute.Key,
                        attribute.TryAdd);
                }
                else
                {
                    RegisterNonKeyed(services, interfaceType, implType, attribute.Lifetime, attribute.TryAdd,
                        attribute.Enumerable);
                }
            }
        }
    }

    private static void MapInjectableDependencyMatchingInterface(ref IServiceCollection services,
        Assembly assembly)
    {
        var entries = GetSortedEntries(assembly, t =>
            t.GetCustomAttributes<InjectableDependencyAttribute>()
                .Where(a => a.ResolveBy == ResolveBy.MatchingInterface));

        foreach (var (implType, attribute) in entries)
        {
            var interfaceName = $"I{implType.Name}";
            var interfaceType = implType.GetInterface(interfaceName);

            if (interfaceType is null || !interfaceType.IsAssignableFrom(implType))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(attribute.Key))
            {
                RegisterKeyed(services, interfaceType, implType, attribute.Lifetime, attribute.Key,
                    attribute.TryAdd);
            }
            else
            {
                RegisterNonKeyed(services, interfaceType, implType, attribute.Lifetime, attribute.TryAdd,
                    attribute.Enumerable);
            }
        }
    }

    private static void MapInjectableDependencySelf(ref IServiceCollection services,
        Assembly assembly)
    {
        var entries = GetSortedEntries(assembly, t =>
            t.GetCustomAttributes<InjectableDependencyAttribute>(inherit: false)
                .Where(a => a.ResolveBy == ResolveBy.Self));

        foreach (var (implType, attribute) in entries)
        {
            if (!string.IsNullOrEmpty(attribute.Key))
            {
                RegisterKeyedSelf(services, implType, attribute.Lifetime, attribute.Key, attribute.TryAdd);
            }
            else
            {
                RegisterNonKeyed(services, implType, implType, attribute.Lifetime, attribute.TryAdd,
                    attribute.Enumerable);
            }
        }
    }

    private static IEnumerable<(Type ImplType, InjectableDependencyAttribute Attribute)> GetSortedEntries(
        Assembly assembly,
        Func<Type, IEnumerable<InjectableDependencyAttribute>> getAttributes)
    {
        return assembly.GetTypes()
            .SelectMany(t => getAttributes(t).Select(a => (ImplType: t, Attribute: a)))
            .OrderBy(e => e.Attribute.Order)
            .ThenBy(e => e.ImplType.Name);
    }

    private static IEnumerable<(Type ImplType, Attribute Attribute)> GetSortedGenericEntries(
        Assembly assembly,
        Type genericAttributeDefinition)
    {
        return assembly.GetTypes()
            .SelectMany(t => GetAttributeImplementations(t, genericAttributeDefinition)
                .Select(a => (ImplType: t, Attribute: a)))
            .Where(e => e.Attribute is IServiceOrder)
            .OrderBy(e => ((IServiceOrder)e.Attribute).Order)
            .ThenBy(e => e.ImplType.Name);
    }

    private static ServiceLifetime ToServiceLifetime(InstanceLifetime lifetime)
    {
        return lifetime switch
        {
            InstanceLifetime.Singleton => ServiceLifetime.Singleton,
            InstanceLifetime.Scoped => ServiceLifetime.Scoped,
            InstanceLifetime.Transient => ServiceLifetime.Transient,
            _ => throw new InvalidEnumArgumentException(nameof(lifetime), (int)lifetime, typeof(InstanceLifetime))
        };
    }

    private static void RegisterNonKeyed(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        InstanceLifetime lifetime,
        bool tryAdd,
        bool enumerable)
    {
        if (tryAdd && enumerable)
        {
            services.TryAddEnumerable(ServiceDescriptor.Describe(serviceType, implementationType,
                ToServiceLifetime(lifetime)));
            return;
        }

        if (tryAdd)
        {
            switch (lifetime)
            {
                case InstanceLifetime.Singleton:
                    services.TryAddSingleton(serviceType, implementationType);
                    break;
                case InstanceLifetime.Scoped:
                    services.TryAddScoped(serviceType, implementationType);
                    break;
                case InstanceLifetime.Transient:
                    services.TryAddTransient(serviceType, implementationType);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }

            return;
        }

        switch (lifetime)
        {
            case InstanceLifetime.Singleton:
                services.AddSingleton(serviceType, implementationType);
                break;
            case InstanceLifetime.Scoped:
                services.AddScoped(serviceType, implementationType);
                break;
            case InstanceLifetime.Transient:
                services.AddTransient(serviceType, implementationType);
                break;
            default:
                throw new InvalidEnumArgumentException();
        }
    }

    private static void RegisterKeyed(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        InstanceLifetime lifetime,
        string key,
        bool tryAdd)
    {
        if (tryAdd)
        {
            switch (lifetime)
            {
                case InstanceLifetime.Singleton:
                    services.TryAddKeyedSingleton(serviceType, key, implementationType);
                    break;
                case InstanceLifetime.Scoped:
                    services.TryAddKeyedScoped(serviceType, key, implementationType);
                    break;
                case InstanceLifetime.Transient:
                    services.TryAddKeyedTransient(serviceType, key, implementationType);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }

            return;
        }

        switch (lifetime)
        {
            case InstanceLifetime.Singleton:
                services.AddKeyedSingleton(serviceType, key, implementationType);
                break;
            case InstanceLifetime.Scoped:
                services.AddKeyedScoped(serviceType, key, implementationType);
                break;
            case InstanceLifetime.Transient:
                services.AddKeyedTransient(serviceType, key, implementationType);
                break;
            default:
                throw new InvalidEnumArgumentException();
        }
    }

    private static void RegisterKeyedSelf(
        IServiceCollection services,
        Type implementationType,
        InstanceLifetime lifetime,
        string key,
        bool tryAdd)
    {
        if (tryAdd)
        {
            switch (lifetime)
            {
                case InstanceLifetime.Singleton:
                    services.TryAddKeyedSingleton(key, implementationType);
                    break;
                case InstanceLifetime.Scoped:
                    services.TryAddKeyedScoped(implementationType, key);
                    break;
                case InstanceLifetime.Transient:
                    services.TryAddKeyedTransient(implementationType, key);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }

            return;
        }

        switch (lifetime)
        {
            case InstanceLifetime.Singleton:
                services.AddKeyedSingleton(key, implementationType);
                break;
            case InstanceLifetime.Scoped:
                services.AddKeyedScoped(implementationType, key);
                break;
            case InstanceLifetime.Transient:
                services.AddKeyedTransient(implementationType, key);
                break;
            default:
                throw new InvalidEnumArgumentException();
        }
    }

    private static IEnumerable<Attribute> GetAttributeImplementations(Type implType, Type targetType)
    {
        return implType.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType && attr.GetType().GetGenericTypeDefinition() == targetType);
    }
}
