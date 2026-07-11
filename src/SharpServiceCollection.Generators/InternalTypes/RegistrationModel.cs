using Microsoft.CodeAnalysis;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.InternalTypes;

internal readonly record struct RegistrationModel
{
    public required INamedTypeSymbol ImplementationType { get; init; }
    public required INamedTypeSymbol? ExplicitServiceType { get; init; }
    public required ResolveBy ResolveBy { get; init; }
    public required InstanceLifetime Lifetime { get; init; }
    public required bool TryAdd { get; init; }
    public required bool Enumerable { get; init; }
    public required string Key { get; init; }
    public required uint Order { get; init; }
    public required Location? Location { get; init; }
}
