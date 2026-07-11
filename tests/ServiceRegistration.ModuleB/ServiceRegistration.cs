using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;

namespace ServiceRegistration.ModuleB;

[ServiceRegistrationItem(Order = 20)]
public sealed class ModuleBServiceRegistration
{
    public Task ExecuteAsync(IServiceCollection services)
    {
        return Task.CompletedTask;
    }
}