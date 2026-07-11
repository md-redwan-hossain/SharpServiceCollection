using SharpServiceCollection.Attributes;
using SharpServiceCollection.Interfaces;

namespace TrialWebApi;

[ServiceRegistrationItem]
public sealed class LocalServiceRegistration : IServiceRegistration
{
    public Task RegisterAsync(IServiceCollection services)
    {
        services.AddSingleton<LocalService>();
        return Task.CompletedTask;
    }
}

public sealed class LocalService
{
}
