using Microsoft.Extensions.DependencyInjection;
using ServiceRegistration.TestShared;
using SharpServiceCollection;

namespace ServiceRegistration.ModuleA;

public sealed class ServiceRegistration : ServiceRegistrationBase
{
    public override int Priority => 10;

    public override Task ExecuteAsync(IServiceCollection services)
    {
        ExecutionLog.Add("A");
        services.AddSingleton(new ModuleAMarker());
        return Task.CompletedTask;
    }
}

public sealed class ModuleAMarker;
