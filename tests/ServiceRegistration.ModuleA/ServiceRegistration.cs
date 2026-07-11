using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;

namespace ServiceRegistration.ModuleA;

[ServiceRegistration<string>(Order = 10)]
public sealed class ModuleAServiceRegistration
{
    public Task ExecuteAsync(IServiceCollection services, string context)
    {
        return Task.CompletedTask;
    }
}