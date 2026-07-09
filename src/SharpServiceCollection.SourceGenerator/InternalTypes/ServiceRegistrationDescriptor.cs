namespace SharpServiceCollection.SourceGenerator.InternalTypes;

internal sealed class ServiceRegistrationDescriptor
{
    public required string ServiceTypeName { get; init; }
    public required string ImplementationTypeName { get; init; }
    public required RegistrationLifetime Lifetime { get; init; }
    public required bool TryAdd { get; init; }
    public required bool Enumerable { get; init; }
    public required string Key { get; init; }
    public required uint Order { get; init; }
    public required string ImplementationNameSortKey { get; init; }
}
