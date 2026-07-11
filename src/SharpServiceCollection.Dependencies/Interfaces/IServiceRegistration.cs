using Microsoft.Extensions.DependencyInjection;

namespace SharpServiceCollection.Interfaces;

public interface IServiceRegistration
{
    public Task RegisterAsync(IServiceCollection services);
}

public interface IServiceRegistration<in TContext>
{
    public Task RegisterAsync(IServiceCollection services, TContext context);
}