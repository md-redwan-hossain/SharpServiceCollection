using Microsoft.Extensions.DependencyInjection;
using ServiceRegistration.TestShared;
using SharpServiceCollection;

namespace ServiceRegistration.ModuleB;

public sealed class ServiceRegistration : ServiceRegistrationBase
{
    public override int Priority => 100;

    public override Task ExecuteAsync(IServiceCollection services)
    {
        ExecutionLog.Add("B");
        services.AddSingleton(new ModuleBMarker());
        return Task.CompletedTask;
    }
}

public sealed class ModuleBMarker;
