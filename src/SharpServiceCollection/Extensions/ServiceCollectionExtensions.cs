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
    [Obsolete($"Use {nameof(AddServicesFromCurrentAssembly)}() method instead.", true)]
    public static IServiceCollection AddServicesBySharpServiceCollection(this IServiceCollection services)
    {
        return AddServicesFromAssembly(services, Assembly.GetCallingAssembly());
    }

    [Obsolete($"Use {nameof(AddServicesFromAssembly)}(Assembly assembly) method instead.", true)]
    public static IServiceCollection AddServicesBySharpServiceCollection(this IServiceCollection services,
        Assembly assembly)
    {
        return AddServicesFromAssembly(services, assembly);
    }

    public static IServiceCollection AddServicesFromAssemblyContaining<T>(this IServiceCollection services)
    {
        return AddServicesFromAssembly(services, typeof(T).Assembly);
    }

    public static IServiceCollection AddServicesFromAssemblyContaining(this IServiceCollection services, Type type)
    {
        return AddServicesFromAssembly(services, type.Assembly);
    }

    public static IServiceCollection AddServicesFromCurrentAssembly(this IServiceCollection services)
    {
        return AddServicesFromAssembly(services, Assembly.GetCallingAssembly());
    }

    public static IServiceCollection AddServicesFromAssembly(this IServiceCollection services,
        Assembly assembly)
    {
        var typesWithAttribute = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes<InjectableDependencyAttribute>().Any())
            .OrderBy(t => t.Name)
            .ToList();

        MapInjectableDependencyMatchingInterface(ref services, typesWithAttribute);
        MapInjectableDependencyImplementedInterface(ref services, typesWithAttribute);
        MapInjectableDependencySelf(ref services, typesWithAttribute);

        MapResolveBy(ref services, assembly);
        MapKeyedResolveBy(ref services, assembly);

        MapTryResolveBy(ref services, assembly);
        MapKeyedTryResolveBy(ref services, assembly);

        MapResolveByMatchingInterface(ref services, assembly);
        MapTryResolveByMatchingInterface(ref services, assembly);

        MapResolveBySelf(ref services, assembly);
        MapTryResolveBySelf(ref services, assembly);

        MapResolveByImplementedInterface(ref services, assembly);
        MapTryResolveByImplementedInterface(ref services, assembly);

        return services;
    }

    private static void MapInjectableDependencyImplementedInterface(ref IServiceCollection services,
        List<Type> typesWithAttribute)
    {
        foreach (var implType in typesWithAttribute)
        {
            var attributes = implType.GetCustomAttributes<InjectableDependencyAttribute>();

            foreach (var attribute in attributes)
            {
                var interfaceTypes = implType.GetInterfaces();

                foreach (var interfaceType in interfaceTypes)
                {
                    if (attribute is null || !interfaceType.IsAssignableFrom(implType))
                    {
                        continue;
                    }

                    // ImplementedInterfaceAndReplace, Keyed
                    if (attribute.ResolveBy == ResolveBy.ImplementedInterfaceAndReplace &&
                        !string.IsNullOrEmpty(attribute.Key))
                    {
                        switch (attribute.Lifetime)
                        {
                            case InstanceLifetime.Singleton:
                                services.AddKeyedSingleton(interfaceType, attribute.Key, implType);
                                break;
                            case InstanceLifetime.Scoped:
                                services.AddKeyedScoped(interfaceType, attribute.Key, implType);
                                break;
                            case InstanceLifetime.Transient:
                                services.AddKeyedTransient(interfaceType, attribute.Key, implType);
                                break;
                            default:
                                throw new InvalidEnumArgumentException();
                        }
                    }

                    // ImplementedInterface, Keyed
                    if (attribute.ResolveBy == ResolveBy.ImplementedInterface &&
                        !string.IsNullOrEmpty(attribute.Key))
                    {
                        switch (attribute.Lifetime)
                        {
                            case InstanceLifetime.Singleton:
                                services.TryAddKeyedSingleton(interfaceType, attribute.Key, implType);
                                break;
                            case InstanceLifetime.Scoped:
                                services.TryAddKeyedScoped(interfaceType, attribute.Key, implType);
                                break;
                            case InstanceLifetime.Transient:
                                services.TryAddKeyedTransient(interfaceType, attribute.Key, implType);
                                break;
                            default:
                                throw new InvalidEnumArgumentException();
                        }
                    }

                    // ImplementedInterfaceAndReplace, Non-Keyed
                    if (attribute.ResolveBy == ResolveBy.ImplementedInterfaceAndReplace &&
                        string.IsNullOrEmpty(attribute.Key))
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

                    // ImplementedInterface, Non-Keyed
                    if (attribute.ResolveBy == ResolveBy.ImplementedInterface &&
                        string.IsNullOrEmpty(attribute.Key))
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
            }
        }
    }

    private static void MapInjectableDependencyMatchingInterface(ref IServiceCollection services,
        List<Type> typesWithAttribute)
    {
        foreach (var implType in typesWithAttribute)
        {
            var attributes = implType.GetCustomAttributes<InjectableDependencyAttribute>();

            foreach (var attribute in attributes)
            {
                var interfaceName = $"I{implType.Name}";
                var interfaceType = implType.GetInterface(interfaceName);

                if (interfaceType is null || !interfaceType.IsAssignableFrom(implType))
                {
                    continue;
                }

                // MatchingInterfaceAndReplace, Keyed
                if (attribute.ResolveBy == ResolveBy.MatchingInterfaceAndReplace &&
                    !string.IsNullOrEmpty(attribute.Key))
                {
                    switch (attribute.Lifetime)
                    {
                        case InstanceLifetime.Singleton:
                            services.AddKeyedSingleton(interfaceType, attribute.Key, implType);
                            break;
                        case InstanceLifetime.Scoped:
                            services.AddKeyedScoped(interfaceType, attribute.Key, implType);
                            break;
                        case InstanceLifetime.Transient:
                            services.AddKeyedTransient(interfaceType, attribute.Key, implType);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }

                // MatchingInterface, Keyed
                if (attribute.ResolveBy == ResolveBy.MatchingInterface &&
                    !string.IsNullOrEmpty(attribute.Key))
                {
                    switch (attribute.Lifetime)
                    {
                        case InstanceLifetime.Singleton:
                            services.TryAddKeyedSingleton(interfaceType, attribute.Key, implType);
                            break;
                        case InstanceLifetime.Scoped:
                            services.TryAddKeyedScoped(interfaceType, attribute.Key, implType);
                            break;
                        case InstanceLifetime.Transient:
                            services.TryAddKeyedTransient(interfaceType, attribute.Key, implType);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }

                // MatchingInterfaceAndReplace, Non-Keyed
                if (attribute.ResolveBy == ResolveBy.MatchingInterfaceAndReplace &&
                    string.IsNullOrEmpty(attribute.Key))
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

                // MatchingInterface, Non-Keyed
                if (attribute.ResolveBy == ResolveBy.MatchingInterface &&
                    string.IsNullOrEmpty(attribute.Key))
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
        }
    }

    private static void MapInjectableDependencySelf(ref IServiceCollection services,
        List<Type> typesWithAttribute)
    {
        foreach (var implType in typesWithAttribute)
        {
            var attributes = implType.GetCustomAttributes(typeof(InjectableDependencyAttribute), inherit: false)
                .Cast<InjectableDependencyAttribute>();

            foreach (var attribute in attributes)
            {
                // SelfAndReplace, Keyed
                if (attribute.ResolveBy == ResolveBy.SelfAndReplace && !string.IsNullOrEmpty(attribute.Key))
                {
                    switch (attribute.Lifetime)
                    {
                        case InstanceLifetime.Singleton:
                            services.AddKeyedSingleton(attribute.Key, implType);
                            break;
                        case InstanceLifetime.Scoped:
                            services.AddKeyedScoped(implType, attribute.Key);
                            break;
                        case InstanceLifetime.Transient:
                            services.AddKeyedTransient(implType, attribute.Key);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }

                // MatchingInterface, Keyed
                if (attribute.ResolveBy == ResolveBy.Self && !string.IsNullOrEmpty(attribute.Key))
                {
                    switch (attribute.Lifetime)
                    {
                        case InstanceLifetime.Singleton:
                            services.TryAddKeyedSingleton(attribute.Key, implType);
                            break;
                        case InstanceLifetime.Scoped:
                            services.TryAddKeyedScoped(implType, attribute.Key);
                            break;
                        case InstanceLifetime.Transient:
                            services.TryAddKeyedTransient(implType, attribute.Key);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }

                // SelfAndReplace, Non-Keyed
                if (attribute.ResolveBy == ResolveBy.SelfAndReplace && string.IsNullOrEmpty(attribute.Key))
                {
                    switch (attribute.Lifetime)
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

                // MatchingInterface, Non-Keyed
                if (attribute.ResolveBy == ResolveBy.Self && string.IsNullOrEmpty(attribute.Key))
                {
                    switch (attribute.Lifetime)
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
        }
    }

    private static void MapResolveByImplementedInterface(ref IServiceCollection services,
        Assembly assembly)
    {
        var typesWithAttribute = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes<ResolveByImplementedInterfaceAttribute>().Any())
            .OrderBy(t => t.Name);

        foreach (var implType in typesWithAttribute)
        {
            var attributes = implType.GetCustomAttributes<ResolveByImplementedInterfaceAttribute>();
            var interfaceTypes = implType.GetInterfaces();

            foreach (var attribute in attributes)
            {
                foreach (var interfaceType in interfaceTypes)
                {
                    if (attribute is not null && interfaceType.IsAssignableFrom(implType))
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
            }
        }
    }

    private static void MapTryResolveByImplementedInterface(ref IServiceCollection services,
        Assembly assembly)
    {
        var typesWithAttribute = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes<TryResolveByImplementedInterfaceAttribute>().Any())
            .OrderBy(t => t.Name);

        foreach (var implType in typesWithAttribute)
        {
            var attributes = implType.GetCustomAttributes<TryResolveByImplementedInterfaceAttribute>();
            var interfaceTypes = implType.GetInterfaces();

            foreach (var attribute in attributes)
            {
                foreach (var interfaceType in interfaceTypes)
                {
                    if (attribute is not null && interfaceType.IsAssignableFrom(implType))
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
            }
        }
    }


    private static void MapResolveBy(ref IServiceCollection services, Assembly assembly)
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
    }

    private static void MapKeyedResolveBy(ref IServiceCollection services, Assembly assembly)
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
    }

    private static void MapResolveByMatchingInterface(ref IServiceCollection services, Assembly assembly)
    {
        var typesWithAttribute = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes<ResolveByMatchingInterfaceAttribute>().Any())
            .OrderBy(t => t.Name);

        foreach (var implType in typesWithAttribute)
        {
            var attributes = implType.GetCustomAttributes<ResolveByMatchingInterfaceAttribute>();
            var interfaceName = $"I{implType.Name}";
            var interfaceType = implType.GetInterface(interfaceName);

            foreach (var attribute in attributes)
            {
                if (interfaceType is not null && interfaceType.IsAssignableFrom(implType))
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
        }
    }

    private static void MapTryResolveByMatchingInterface(ref IServiceCollection services,
        Assembly assembly)
    {
        var typesWithAttribute = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes<TryResolveByMatchingInterfaceAttribute>().Any())
            .OrderBy(t => t.Name);

        foreach (var implType in typesWithAttribute)
        {
            var attributes = implType.GetCustomAttributes<TryResolveByMatchingInterfaceAttribute>();
            var interfaceName = $"I{implType.Name}";
            var interfaceType = implType.GetInterface(interfaceName);

            foreach (var attribute in attributes)
            {
                if (interfaceType is not null && interfaceType.IsAssignableFrom(implType))
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
        }
    }

    private static void MapTryResolveBy(ref IServiceCollection services, Assembly assembly)
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
    }

    private static void MapKeyedTryResolveBy(ref IServiceCollection services, Assembly assembly)
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
    }

    private static void MapResolveBySelf(ref IServiceCollection services, Assembly assembly)
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
    }

    private static void MapTryResolveBySelf(ref IServiceCollection services, Assembly assembly)
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