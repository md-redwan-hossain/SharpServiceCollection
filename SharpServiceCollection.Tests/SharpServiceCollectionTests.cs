using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Extensions;
using SharpServiceCollection.Tests.TestData.ConcreteTypes;
using SharpServiceCollection.Tests.TestData.Interfaces;
using Shouldly;

namespace SharpServiceCollection.Tests;

public class SharpServiceCollectionTests
{
    [Fact]
    public void ScopedDependencyForInjectableBy()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var descriptor = serviceCollection
            .FirstOrDefault(d => d.ServiceType == typeof(IScopedDependencyForInjectableBy));

        // Assert
        var service = serviceProvider.GetService<IScopedDependencyForInjectableBy>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ScopedDependencyForInjectableBy>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
    
    [Fact]
    public void ScopedDependencyForInjectable_ResolveBy_Self()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddServicesFromAssembly(assembly);
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
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
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
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
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
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddServicesFromAssembly(assembly);
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
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
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
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddServicesFromAssembly(assembly);
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
    public void TransientDependency_ResolveByMatchingInterface()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
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
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddServicesFromAssembly(assembly);
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
    public void ScopedDemoType_ResolveBy_IScopedDemoService()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IScopedDemoService));

        // Assert
        var service = serviceProvider.GetService<IScopedDemoService>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ScopedDemoType>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void ScopedDemoType_KeyedResolveBy_IKeyedScopedDemoService()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IKeyedScopedDemoService));

        // Assert
        var service = serviceProvider.GetKeyedService<IKeyedScopedDemoService>("keyed");
        service.ShouldNotBeNull();
        service.ShouldBeOfType<ScopedDemoType>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void ScopedDemoType_KeyedResolveBy_IKeyedScopedDemoService_ShouldBeNull()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetKeyedService<IKeyedScopedDemoService>("wrong_key");
        service.ShouldBeNull();
    }

    [Fact]
    public void Baz_TryResolveBy_ITryResolver()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(ITryResolver));

        // Assert
        var service = serviceProvider.GetService<ITryResolver>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<Bar>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }


    [Fact]
    public void Foo_TryResolveBy_IResolver()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IResolver));

        // Assert
        var service = serviceProvider.GetService<IResolver>();
        service.ShouldNotBeNull();
        service.ShouldBeOfType<Foo>();

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void FooBarBaz_ResolveByImplementedInterface_IFoo_IBar_IBaz()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();
        List<Type> types = [typeof(IFoo), typeof(IBar), typeof(IBaz)];

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        foreach (var type in types)
        {
            var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == type);
            var service = serviceProvider.GetService(type);

            // Assert
            service.ShouldNotBeNull();
            service.ShouldBeOfType<FooBarBaz>();

            descriptor.ShouldNotBeNull();
            descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        }
    }

    [Fact]
    public void FooBarBaz_TryResolveByImplementedInterface_IFoo_IBar_IBaz()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();
        List<Type> types = [typeof(IFoo), typeof(IBar), typeof(IBaz), typeof(IXyz)];

        // Act
        serviceCollection.AddServicesFromAssembly(assembly);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        foreach (var type in types)
        {
            var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == type);
            var service = serviceProvider.GetService(type);

            // Assert
            service.ShouldNotBeNull();

            if (type == typeof(IXyz))
            {
                service.ShouldBeOfType<FooBarBazWithTry>();
                service.ShouldNotBeOfType<FooBarBaz>();
            }
            else
            {
                service.ShouldNotBeOfType<FooBarBazWithTry>();
                service.ShouldBeOfType<FooBarBaz>();
            }

            descriptor.ShouldNotBeNull();
            descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        }
    }
}