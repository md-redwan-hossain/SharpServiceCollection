using Microsoft.Extensions.DependencyInjection;
using ServiceRegistration.ModuleA;
using ServiceRegistration.ModuleB;
using ServiceRegistration.TestShared;
using SharpServiceCollection.Generated;
using Shouldly;
using Xunit;

namespace SharpServiceCollection.Tests;

public class ServiceRegistrationApiTests
{
    [Fact]
    public async Task ExecuteServiceRegistrationsAsync_InvokesNonGenericModules_ByPriorityDescending()
    {
        ExecutionLog.Clear();
        var services = new ServiceCollection();

        await services.ExecuteServiceRegistrationsAsync();

        ExecutionLog.Snapshot().ShouldBe(["B", "A"]);
        services.Any(d => d.ServiceType == typeof(ModuleAMarker)).ShouldBeTrue();
        services.Any(d => d.ServiceType == typeof(ModuleBMarker)).ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteServiceRegistrationsAsync_WithContext_InvokesMatchingGenericModules()
    {
        ExecutionLog.Clear();
        var services = new ServiceCollection();
        var context = new TestRegistrationContext();

        await services.ExecuteServiceRegistrationsAsync(context);

        context.Calls.ShouldBe(["Generic"]);
        ExecutionLog.Snapshot().ShouldBe(["Generic"]);
    }
}
