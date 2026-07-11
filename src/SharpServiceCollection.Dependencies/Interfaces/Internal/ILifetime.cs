using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Interfaces.Internal;

internal interface IServiceLifetime
{
    InstanceLifetime Lifetime { get; }
}