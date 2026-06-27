using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Interfaces;

internal interface IServiceLifetime
{
    InstanceLifetime Lifetime { get; }
}