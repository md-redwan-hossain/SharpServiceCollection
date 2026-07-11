using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Generated;
using SharpServiceCollection.Tests.TestData.ConcreteTypes;
using SharpServiceCollection.Tests.TestData.Interfaces;
using Shouldly;
using Xunit;

namespace SharpServiceCollection.Tests;

public class SourceGeneratedApiTests
{
    [Fact]
    public void ScopedDependencyForInjectableBy()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var descriptor = serviceCollection
            .FirstOrDefault(d => d.ServiceType == typeof(IScopedDependencyForInjectableGeneric));

        // Assert
        var service = serviceProvider.GetService<IScopedDependencyForInjectableGeneric>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ScopedDependencyForInjectableGeneric>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void ScopedDependencyForInjectableBy_Keyed()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var descriptor = serviceCollection
            .FirstOrDefault(d => d.ServiceType == typeof(IScopedDependencyForInjectableGeneric));

        // Assert
        var service = serviceProvider.GetKeyedService<IScopedDependencyForInjectableGeneric>("key");
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ScopedDependencyForInjectableGeneric>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void ScopedDependencyForInjectable_ResolveBy_Self()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAttributedServices();
        var serviceProvider = services.BuildServiceProvider();
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ScopedDependencyForInjectable));

        // Assert
        var service = serviceProvider.GetService<ScopedDependencyForInjectable>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ScopedDependencyForInjectable>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void ScopedDependencyForInjectable_ResolveBy_MatchingInterface()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var descriptor = serviceCollection
            .FirstOrDefault(d => d.ServiceType == typeof(IScopedDependencyForInjectable));

        // Assert
        var service = serviceProvider.GetService<IScopedDependencyForInjectable>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ScopedDependencyForInjectable>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void ScopedDependency_ResolveByMatchingInterface()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IScopedDependency));

        // Assert
        var service = serviceProvider.GetService<IScopedDependency>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ScopedDependency>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void ScopedDependency_ResolveBySelf()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAttributedServices();
        var serviceProvider = services.BuildServiceProvider();
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ScopedDependency));

        // Assert
        var service = serviceProvider.GetService<ScopedDependency>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ScopedDependency>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void SingletonDependency_ResolveByMatchingInterface()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(ISingletonDependency));

        // Assert
        var service = serviceProvider.GetService<ISingletonDependency>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<SingletonDependency>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void SingletonDependency_ResolveBySelf()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAttributedServices();
        var serviceProvider = services.BuildServiceProvider();
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(SingletonDependency));

        // Assert
        var service = serviceProvider.GetService<SingletonDependency>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<SingletonDependency>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void SelfResolvableDependency_TryResolveBySelf_Scoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAttributedServices();
        var serviceProvider = services.BuildServiceProvider();
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(SelfResolvableDependency));

        // Assert
        var service = serviceProvider.GetService<SelfResolvableDependency>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<SelfResolvableDependency>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void SelfResolvableDependency_TryResolveBySelf_Transient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAttributedServices();
        var serviceProvider = services.BuildServiceProvider();
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IKeyedTransientDependency));

        // Assert
        var service = serviceProvider.GetKeyedService<IKeyedTransientDependency>("key-111");
        service.ShouldNotBeNull();
        service.ShouldBeOfType<SelfResolvableDependency>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void TransientDependency_ResolveByMatchingInterface()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(ITransientDependency));

        // Assert
        var service = serviceProvider.GetService<ITransientDependency>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<TransientDependency>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void TransientDependency_ResolveBySelf()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAttributedServices();
        var serviceProvider = services.BuildServiceProvider();
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TransientDependency));

        // Assert
        var service = serviceProvider.GetService<TransientDependency>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<TransientDependency>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void KeyedScopedDependency_ResolveBy_IScopedKeyedDependency()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IScopedKeyedDependency));

        // Assert
        var service = serviceProvider.GetService<IScopedKeyedDependency>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<KeyedScopedDependency>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void KeyedScopedDependency_KeyedResolveBy_IKeyedScopedDependency()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IKeyedScopedDependency));

        // Assert
        var service = serviceProvider.GetKeyedService<IKeyedScopedDependency>("keyed");
        service.ShouldNotBeNull();
        service.ShouldBeOfType<KeyedScopedDependency>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void KeyedScopedDependency_KeyedResolveBy_IKeyedScopedDependency_ShouldBeNull()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetKeyedService<IKeyedScopedDependency>("wrong_key");
        service.ShouldBeNull();
    }

    [Fact]
    public void FirstRegisteredResolver_TryResolveBy_ITryResolver()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(ITryResolver));

        // Assert
        var service = serviceProvider.GetService<ITryResolver>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<FirstRegisteredResolver>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }


    [Fact]
    public void LastRegisteredResolver_TryResolveBy_IResolver()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IResolver));

        // Assert
        var service = serviceProvider.GetService<IResolver>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<LastRegisteredResolver>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void ImplementedInterfaceNonTryService_ResolveByImplementedInterface_Markers()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        List<Type> types =
        [
            typeof(IResolvableMarkerAlpha),
            typeof(IResolvableMarkerBeta),
            typeof(IResolvableMarkerGamma)
        ];

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        foreach (var type in types)
        {
            var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == type);
            var service = serviceProvider.GetService(type);

            // Assert
            service.ShouldNotBeNull();
            service.ShouldBeOfType<ImplementedInterfaceNonTryService>();

            descriptor.ShouldNotBeNull();
            descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        }
    }

    [Fact]
    public void ImplementedInterfaceTryService_TryResolveByImplementedInterface_Markers()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        List<Type> types =
        [
            typeof(IResolvableMarkerAlpha),
            typeof(IResolvableMarkerBeta),
            typeof(IResolvableMarkerGamma),
            typeof(IResolvableMarkerDelta)
        ];

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        foreach (var type in types)
        {
            var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == type);
            var service = serviceProvider.GetService(type);

            // Assert
            service.ShouldNotBeNull();

            if (type == typeof(IResolvableMarkerDelta))
            {
                service.ShouldBeOfType<ImplementedInterfaceTryService>();
                service.ShouldNotBeOfType<ImplementedInterfaceNonTryService>();
            }
            else
            {
                service.ShouldNotBeOfType<ImplementedInterfaceTryService>();
                service.ShouldBeOfType<ImplementedInterfaceNonTryService>();
            }

            descriptor.ShouldNotBeNull();
            descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        }
    }

    [Fact]
    public void EnumerablePlugins_TryAddEnumerable_RegistersMultipleImplementations()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var plugins = serviceProvider.GetServices<IEnumerablePlugin>().ToList();
        var descriptors = serviceCollection
            .Where(d => d.ServiceType == typeof(IEnumerablePlugin))
            .ToList();

        // Assert
        plugins.Count.ShouldBe(2);
        plugins.ShouldContain(p => p is EnumerablePluginPrimary);
        plugins.ShouldContain(p => p is EnumerablePluginSecondary);
        descriptors.Count.ShouldBe(2);
    }

    [Fact]
    public void OrderTryAdd_LowerOrderWins_OverClassName()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Assert — ZebraWinsTryAddResolver (Order = 1) wins over AlphaLosesTryAddResolver (Order = 2)
        // despite Alpha sorting first by class name
        var service = serviceProvider.GetService<IOrderTryResolver>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ZebraWinsTryAddResolver>();
    }

    [Fact]
    public void OrderAdd_HigherOrderWins_OverClassName()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddAttributedServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Assert — AlphaWinsAddResolver (Order = 2) wins over ZebraLosesAddResolver (Order = 1)
        // despite Zebra sorting first by class name
        var service = serviceProvider.GetService<IOrderAddResolver>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<AlphaWinsAddResolver>();
    }


    [Fact]
    public void AddAttributedServicesFromSharpServiceCollectionTests_RegistersMatchingInterface()
    {
        var services = new ServiceCollection();

        services.AddAttributedServicesFrom_SharpServiceCollectionTests();
        var provider = services.BuildServiceProvider();

        var service = provider.GetService<IScopedDependency>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ScopedDependency>();
    }
}