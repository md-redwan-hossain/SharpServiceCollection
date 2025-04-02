using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SharpServiceCollection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServicesBySharpServiceCollection(this IServiceCollection services)
    {
        return AddServicesBySharpServiceCollection(services, Assembly.GetExecutingAssembly());
    }

    public static IServiceCollection AddServicesBySharpServiceCollection(this IServiceCollection services,
        Assembly assembly)
    {
        services = MapResolveFrom(services, assembly);
        services = MapResolveFromWithKey(services, assembly);

        services = MapTryResolveFrom(services, assembly);
        services = MapTryResolveFromWithKey(services, assembly);

        services = MapResolveFromSelf(services, assembly);

        return services;
    }

    private static IServiceCollection MapResolveFrom(IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = GetGenericAttributes(assembly, typeof(ResolveFromAttribute<>));

        foreach (var implType in typesWithAttribute)
        {
            var attributes = GetAttributeImplementations(implType, typeof(ResolveFromAttribute<>));

            foreach (var attribute in attributes)
            {
                // Retrieve the type argument T from ResolveFromAttribute<T>.
                var resolverType = attribute.GetType().GetGenericArguments()[0];

                var lifetimeProperty = attribute.GetType()
                    .GetProperty(nameof(ResolveFromAttribute<byte>.Lifetime));


                if (lifetimeProperty?.GetValue(attribute) is InstanceLifetime lifetime)
                {
                    // Register the service with the appropriate lifetime.
                    switch (lifetime)
                    {
                        case InstanceLifetime.Singleton:
                            services.AddSingleton(resolverType, implType);
                            break;
                        case InstanceLifetime.Scoped:
                            services.AddScoped(resolverType, implType);
                            break;
                        case InstanceLifetime.Transient:
                            services.AddTransient(resolverType, implType);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }
            }
        }

        return services;
    }

    private static IServiceCollection MapResolveFromWithKey(IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = GetGenericAttributes(assembly, typeof(ResolveFromWithKeyAttribute<>));

        foreach (var implType in typesWithAttribute)
        {
            var attributes = GetAttributeImplementations(implType, typeof(ResolveFromWithKeyAttribute<>));

            foreach (var attribute in attributes)
            {
                // Retrieve the type argument T from ResolveFromAttribute<T>.
                var resolverType = attribute.GetType().GetGenericArguments()[0];

                var lifetimeProperty = attribute.GetType()
                    .GetProperty(nameof(ResolveFromWithKeyAttribute<byte>.Lifetime));

                var keyProperty = attribute.GetType()
                    .GetProperty(nameof(ResolveFromWithKeyAttribute<byte>.Key));


                if (lifetimeProperty?.GetValue(attribute) is InstanceLifetime lifetime &&
                    keyProperty?.GetValue(attribute) is string key && string.IsNullOrEmpty(key) is false)
                {
                    // Register the service with the appropriate lifetime.
                    switch (lifetime)
                    {
                        case InstanceLifetime.Singleton:
                            services.AddKeyedSingleton(resolverType, key, implType);
                            break;
                        case InstanceLifetime.Scoped:
                            services.AddKeyedScoped(resolverType, key, implType);
                            break;
                        case InstanceLifetime.Transient:
                            services.AddKeyedTransient(resolverType, key, implType);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }
            }
        }

        return services;
    }

    private static IServiceCollection MapTryResolveFrom(IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = GetGenericAttributes(assembly, typeof(TryResolveFromAttribute<>));

        foreach (var implType in typesWithAttribute)
        {
            var attributes = GetAttributeImplementations(implType, typeof(TryResolveFromAttribute<>));

            foreach (var attribute in attributes)
            {
                // Retrieve the type argument T from ResolveFromAttribute<T>.
                var resolverType = attribute.GetType().GetGenericArguments()[0];

                var lifetimeProperty = attribute.GetType()
                    .GetProperty(nameof(TryResolveFromAttribute<byte>.Lifetime));

                if (lifetimeProperty?.GetValue(attribute) is InstanceLifetime lifetime)
                {
                    // Register the service with the appropriate lifetime.
                    switch (lifetime)
                    {
                        case InstanceLifetime.Singleton:
                            services.TryAddSingleton(resolverType, implType);
                            break;
                        case InstanceLifetime.Scoped:
                            services.TryAddScoped(resolverType, implType);
                            break;
                        case InstanceLifetime.Transient:
                            services.TryAddTransient(resolverType, implType);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }
            }
        }

        return services;
    }

    private static IServiceCollection MapTryResolveFromWithKey(IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = GetGenericAttributes(assembly, typeof(TryResolveFromWithKeyAttribute<>));

        foreach (var implType in typesWithAttribute)
        {
            var attributes = GetAttributeImplementations(implType, typeof(TryResolveFromWithKeyAttribute<>));

            foreach (var attribute in attributes)
            {
                // Retrieve the type argument T from ResolveFromAttribute<T>.
                var resolverType = attribute.GetType().GetGenericArguments()[0];

                var lifetimeProperty = attribute.GetType()
                    .GetProperty(nameof(TryResolveFromWithKeyAttribute<byte>.Lifetime));

                var keyProperty = attribute.GetType()
                    .GetProperty(nameof(TryResolveFromWithKeyAttribute<byte>.Key));

                if (lifetimeProperty?.GetValue(attribute) is InstanceLifetime lifetime &&
                    keyProperty?.GetValue(attribute) is string key && string.IsNullOrEmpty(key) is false)
                {
                    // Register the service with the appropriate lifetime.
                    switch (lifetime)
                    {
                        case InstanceLifetime.Singleton:
                            services.TryAddKeyedSingleton(resolverType, key, implType);
                            break;
                        case InstanceLifetime.Scoped:
                            services.TryAddKeyedScoped(resolverType, key, implType);
                            break;
                        case InstanceLifetime.Transient:
                            services.TryAddKeyedTransient(resolverType, key, implType);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }
            }
        }

        return services;
    }

    private static IServiceCollection MapResolveFromSelf(IServiceCollection services, Assembly assembly)
    {
        // Register services decorated with ResolveFromSelfAttribute
        var typesWithResolveFromSelf = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(ResolveFromSelfAttribute), false).Length != 0);

        foreach (var implType in typesWithResolveFromSelf)
        {
            var attributes = implType.GetCustomAttributes(typeof(ResolveFromSelfAttribute), inherit: false)
                .Cast<ResolveFromSelfAttribute>();

            foreach (var attribute in attributes)
            {
                var lifetime = attribute.Lifetime;
                // Register the type as itself.
                switch (lifetime)
                {
                    case InstanceLifetime.Singleton:
                        services.AddSingleton(implType);
                        break;
                    case InstanceLifetime.Scoped:
                        services.AddScoped(implType);
                        break;
                    case InstanceLifetime.Transient:
                        services.AddTransient(implType);
                        break;
                    default:
                        throw new InvalidEnumArgumentException();
                }
            }
        }

        return services;
    }

    private static IEnumerable<Type> GetGenericAttributes(Assembly assembly, Type targetType)
    {
        return assembly.GetTypes()
            .Where(t => t.GetCustomAttributes().Any(attr =>
                attr.GetType().IsGenericType && attr.GetType().GetGenericTypeDefinition() == targetType));
    }

    private static IEnumerable<Attribute> GetAttributeImplementations(Type implType, Type targetType)
    {
        return implType.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType && attr.GetType().GetGenericTypeDefinition() == targetType);
    }
}