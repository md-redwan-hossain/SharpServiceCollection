using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Interfaces;

public interface IServiceLifetime
{
    InstanceLifetime Lifetime { get; }
}