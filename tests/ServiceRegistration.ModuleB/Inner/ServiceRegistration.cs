using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;

namespace ServiceRegistration.ModuleB.Inner;

[ServiceRegistration<int>(Order = 5)]
public sealed class InnerServiceRegistration
{
    public Task ExecuteAsync(IServiceCollection services, int context)
    {
        return Task.CompletedTask;
    }
}