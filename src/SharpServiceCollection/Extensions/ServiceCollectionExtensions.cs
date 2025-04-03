using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;


namespace SharpServiceCollection.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServicesBySharpServiceCollection(this IServiceCollection services)
    {
        return AddServicesBySharpServiceCollection(services, Assembly.GetCallingAssembly());
    }

    public static IServiceCollection AddServicesBySharpServiceCollection(this IServiceCollection services,
        Assembly assembly)
    {
        services = MapResolveBy(services, assembly);
        services = MapKeyedResolveBy(services, assembly);

        services = MapTryResolveBy(services, assembly);
        services = MapKeyedTryResolveBy(services, assembly);

        services = MapResolveByMatchingInterface(services, assembly);
        services = MapTryResolveByMatchingInterface(services, assembly);

        services = MapResolveBySelf(services, assembly);
        services = MapTryResolveBySelf(services, assembly);

        return services;
    }

    private static IServiceCollection MapResolveBy(IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = GetGenericAttributes(assembly, typeof(ResolveByAttribute<>));

        foreach (var implType in typesWithAttribute)
        {
            var attributes = GetAttributeImplementations(implType, typeof(ResolveByAttribute<>));

            foreach (var attribute in attributes)
            {
                var resolverType = attribute.GetType().GetGenericArguments()[0];

                var lifetimeProperty = attribute.GetType()
                    .GetProperty(nameof(ResolveByAttribute<byte>.Lifetime));


                if (lifetimeProperty?.GetValue(attribute) is InstanceLifetime lifetime)
                {
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

    private static IServiceCollection MapKeyedResolveBy(IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = GetGenericAttributes(assembly, typeof(KeyedResolveByAttribute<>));

        foreach (var implType in typesWithAttribute)
        {
            var attributes = GetAttributeImplementations(implType, typeof(KeyedResolveByAttribute<>));

            foreach (var attribute in attributes)
            {
                var resolverType = attribute.GetType().GetGenericArguments()[0];

                var lifetimeProperty = attribute.GetType()
                    .GetProperty(nameof(KeyedResolveByAttribute<byte>.Lifetime));

                var keyProperty = attribute.GetType()
                    .GetProperty(nameof(KeyedResolveByAttribute<byte>.Key));


                if (lifetimeProperty?.GetValue(attribute) is InstanceLifetime lifetime &&
                    keyProperty?.GetValue(attribute) is string key && string.IsNullOrEmpty(key) is false)
                {
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

    private static IServiceCollection MapResolveByMatchingInterface(IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ResolveByMatchingInterfaceAttribute>() is not null);

        foreach (var implType in typesWithAttribute)
        {
            var attribute = implType.GetCustomAttribute<ResolveByMatchingInterfaceAttribute>();
            var interfaceName = $"I{implType.Name}";
            var interfaceType = implType.GetInterface(interfaceName);

            if (interfaceType is not null && attribute is not null && interfaceType.IsAssignableFrom(implType))
            {
                switch (attribute.Lifetime)
                {
                    case InstanceLifetime.Singleton:
                        services.AddSingleton(interfaceType, implType);
                        break;
                    case InstanceLifetime.Scoped:
                        services.AddScoped(interfaceType, implType);
                        break;
                    case InstanceLifetime.Transient:
                        services.AddTransient(interfaceType, implType);
                        break;
                    default:
                        throw new InvalidEnumArgumentException();
                }
            }
        }

        return services;
    }

    private static IServiceCollection MapTryResolveByMatchingInterface(IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<TryResolveByMatchingInterfaceAttribute>() is not null);

        foreach (var implType in typesWithAttribute)
        {
            var attribute = implType.GetCustomAttribute<TryResolveByMatchingInterfaceAttribute>();
            var interfaceName = $"I{implType.Name}";
            var interfaceType = implType.GetInterface(interfaceName);

            if (interfaceType is not null && attribute is not null && interfaceType.IsAssignableFrom(implType))
            {
                switch (attribute.Lifetime)
                {
                    case InstanceLifetime.Singleton:
                        services.TryAddSingleton(interfaceType, implType);
                        break;
                    case InstanceLifetime.Scoped:
                        services.TryAddScoped(interfaceType, implType);
                        break;
                    case InstanceLifetime.Transient:
                        services.TryAddTransient(interfaceType, implType);
                        break;
                    default:
                        throw new InvalidEnumArgumentException();
                }
            }
        }

        return services;
    }

    private static IServiceCollection MapTryResolveBy(IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = GetGenericAttributes(assembly, typeof(TryResolveByAttribute<>));

        foreach (var implType in typesWithAttribute)
        {
            var attributes = GetAttributeImplementations(implType, typeof(TryResolveByAttribute<>));

            foreach (var attribute in attributes)
            {
                var resolverType = attribute.GetType().GetGenericArguments()[0];

                var lifetimeProperty = attribute.GetType()
                    .GetProperty(nameof(TryResolveByAttribute<byte>.Lifetime));

                if (lifetimeProperty?.GetValue(attribute) is InstanceLifetime lifetime)
                {
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

    private static IServiceCollection MapKeyedTryResolveBy(IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = GetGenericAttributes(assembly, typeof(KeyedTryResolveByAttribute<>));

        foreach (var implType in typesWithAttribute)
        {
            var attributes = GetAttributeImplementations(implType, typeof(KeyedTryResolveByAttribute<>));

            foreach (var attribute in attributes)
            {
                var resolverType = attribute.GetType().GetGenericArguments()[0];

                var lifetimeProperty = attribute.GetType()
                    .GetProperty(nameof(KeyedTryResolveByAttribute<byte>.Lifetime));

                var keyProperty = attribute.GetType()
                    .GetProperty(nameof(KeyedTryResolveByAttribute<byte>.Key));

                if (lifetimeProperty?.GetValue(attribute) is InstanceLifetime lifetime &&
                    keyProperty?.GetValue(attribute) is string key && string.IsNullOrEmpty(key) is false)
                {
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

    private static IServiceCollection MapResolveBySelf(IServiceCollection services, Assembly assembly)
    {
        var typesWithResolveFromSelf = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(ResolveBySelfAttribute), false).Length != 0);

        foreach (var implType in typesWithResolveFromSelf)
        {
            var attributes = implType.GetCustomAttributes(typeof(ResolveBySelfAttribute), inherit: false)
                .Cast<ResolveBySelfAttribute>();

            foreach (var attribute in attributes)
            {
                var lifetime = attribute.Lifetime;
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

    private static IServiceCollection MapTryResolveBySelf(IServiceCollection services, Assembly assembly)
    {
        var typesWithResolveFromSelf = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(TryResolveBySelfAttribute), false).Length != 0);

        foreach (var implType in typesWithResolveFromSelf)
        {
            var attributes = implType.GetCustomAttributes(typeof(TryResolveBySelfAttribute), inherit: false)
                .Cast<TryResolveBySelfAttribute>();

            foreach (var attribute in attributes)
            {
                var lifetime = attribute.Lifetime;
                switch (lifetime)
                {
                    case InstanceLifetime.Singleton:
                        services.TryAddSingleton(implType);
                        break;
                    case InstanceLifetime.Scoped:
                        services.TryAddScoped(implType);
                        break;
                    case InstanceLifetime.Transient:
                        services.TryAddTransient(implType);
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
                attr.GetType().IsGenericType && attr.GetType().GetGenericTypeDefinition() == targetType))
            .OrderBy(t => t.Name);
    }

    private static IEnumerable<Attribute> GetAttributeImplementations(Type implType, Type targetType)
    {
        return implType.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType && attr.GetType().GetGenericTypeDefinition() == targetType);
    }
}