using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection;

namespace ServiceRegistration.ModuleA;

public sealed class ServiceRegistration : ServiceRegistrationBase
{
    public override int Priority => 10;

    public override Task ExecuteAsync(IServiceCollection services)
    {
        return Task.CompletedTask;
    }
}

