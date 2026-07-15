using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Interfaces;

namespace ServiceRegistration.ModuleB;

[ServiceRegistrationItem(Priority = 20)]
public sealed class ModuleBServiceRegistration : IServiceRegistration<TimeOnly>
{
    public Task RegisterAsync(IServiceCollection services, TimeOnly context)
    {
        return Task.CompletedTask;
    }
}