using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Interfaces;

namespace ServiceRegistration.ModuleA;

[ServiceRegistrationItem(Order = 10)]
public class ModuleAServiceRegistration
{
    public Task RegisterAsync(IServiceCollection services, string context)
    {
        return Task.CompletedTask;
    }
}