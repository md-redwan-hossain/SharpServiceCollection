using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Extensions;
using SharpServiceCollection.Tests.TestData.WithConventionalName;
using SharpServiceCollection.Tests.TestData.WithoutConventionalName;
using Shouldly;

namespace SharpServiceCollection.Tests;

public class SharpServiceCollectionTests
{
    [Fact]
    public void ScopedDependency_ResolveByMatchingInterface()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceCollection.AddServicesBySharpServiceCollection(assembly);
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
        services.AddServicesBySharpServiceCollection(assembly);
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
        serviceCollection.AddServicesBySharpServiceCollection(assembly);
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
        services.AddServicesBySharpServiceCollection(assembly);
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
        serviceCollection.AddServicesBySharpServiceCollection(assembly);
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
        services.AddServicesBySharpServiceCollection(assembly);
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
        serviceCollection.AddServicesBySharpServiceCollection(assembly);
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
        serviceCollection.AddServicesBySharpServiceCollection(assembly);
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
        serviceCollection.AddServicesBySharpServiceCollection(assembly);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetKeyedService<IKeyedScopedDemoService>("wrong_key");
        service.ShouldBeNull();
    }
}