using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SharpServiceCollection;

public abstract class ServiceRegistrationBase
{
    public abstract Task ExecuteAsync(IServiceCollection services);

    public virtual int Priority => 1;
}

public abstract class ServiceRegistrationBase<T>
{
    public abstract Task ExecuteAsync(IServiceCollection services, T context);

    public virtual int Priority => 1;
}
