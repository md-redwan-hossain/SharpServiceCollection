using Microsoft.Extensions.DependencyInjection;
using ServiceRegistration.TestShared;
using SharpServiceCollection;

namespace ServiceRegistration.ModuleGeneric;

public sealed class ServiceRegistration : ServiceRegistrationBase<TestRegistrationContext>
{
    public override int Priority => 50;

    public override Task ExecuteAsync(IServiceCollection services, TestRegistrationContext context)
    {
        context.Calls.Add("Generic");
        ExecutionLog.Add("Generic");
        return Task.CompletedTask;
    }
}
