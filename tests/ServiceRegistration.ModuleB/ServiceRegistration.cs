using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Interfaces;

namespace ServiceRegistration.ModuleB;

[ServiceRegistrationItem(Order = 20)]
public sealed class ModuleBServiceRegistration : IServiceRegistration<int>
{
    public Task RegisterAsync(IServiceCollection services, int context)
    {
        return Task.CompletedTask;
    }
}