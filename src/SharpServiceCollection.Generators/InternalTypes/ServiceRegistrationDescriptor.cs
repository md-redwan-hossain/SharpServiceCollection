using SharpServiceCollection.Enums;

namespace SharpServiceCollection.InternalTypes;

internal readonly record struct ServiceRegistrationDescriptor
{
    public required string ServiceTypeName { get; init; }
    public required string ImplementationTypeName { get; init; }
    public required InstanceLifetime Lifetime { get; init; }
    public required bool TryAdd { get; init; }
    public required bool Enumerable { get; init; }
    public required string Key { get; init; }
    public required int Priority { get; init; }
    public required string ImplementationNameSortKey { get; init; }
}
