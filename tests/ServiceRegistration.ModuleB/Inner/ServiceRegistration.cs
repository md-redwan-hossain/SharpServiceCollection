using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Interfaces;

namespace ServiceRegistration.ModuleB.Inner;

[ServiceRegistrationItem]
public sealed class ModuleAServiceRegistration : IServiceRegistration<(string name, decimal code)>
{
    public Task RegisterAsync(IServiceCollection services, (string name, decimal code) context)
    {
        return Task.CompletedTask;
    }
}