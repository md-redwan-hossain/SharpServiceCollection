using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;

namespace ServiceRegistration.ModuleB;

[ServiceRegistration(Order = 20)]
public sealed class ModuleBServiceRegistration
{
    public Task ExecuteAsync(IServiceCollection services)
    {
        return Task.CompletedTask;
    }
}