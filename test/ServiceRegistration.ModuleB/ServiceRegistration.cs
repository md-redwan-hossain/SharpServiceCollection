using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection;

namespace ServiceRegistration.ModuleB;

public sealed class ServiceRegistration : ServiceRegistrationBase
{
    public override int Priority => 100;

    public override Task ExecuteAsync(IServiceCollection services)
    {
        return Task.CompletedTask;
    }
}

