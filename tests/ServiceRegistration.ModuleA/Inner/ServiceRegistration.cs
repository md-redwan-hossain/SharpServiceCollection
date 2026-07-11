using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Interfaces;

namespace ServiceRegistration.ModuleA.Inner;

[ServiceRegistrationItem]
public sealed class ModuleAServiceRegistration : IServiceRegistration<(string name, int code)>
{
    public Task RegisterAsync(IServiceCollection services, (string name, int code) context)
    {
        return Task.CompletedTask;
    }
}