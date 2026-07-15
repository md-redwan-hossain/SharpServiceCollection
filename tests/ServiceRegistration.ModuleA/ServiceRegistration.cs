using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Interfaces;

namespace ServiceRegistration.ModuleA;

[ServiceRegistrationItem(Priority = 10)]
public sealed class ModuleAServiceRegistration: IServiceRegistration<string>
{
    public Task RegisterAsync(IServiceCollection services, string context)
    {
        return Task.CompletedTask;
    }
}