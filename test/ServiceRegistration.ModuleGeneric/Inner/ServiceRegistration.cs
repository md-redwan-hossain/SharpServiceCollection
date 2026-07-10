using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection;

namespace ServiceRegistration.ModuleGeneric.Inner;

public sealed class ServiceRegistration : ServiceRegistrationBase<string>
{
    public override int Priority => 50;

    public override Task ExecuteAsync(IServiceCollection services, string context)
    {
        return Task.CompletedTask;
    }
}
