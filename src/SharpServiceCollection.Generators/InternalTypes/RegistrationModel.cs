using Microsoft.CodeAnalysis;

namespace SharpServiceCollection.Generators.InternalTypes;

internal enum RegistrationResolveBy
{
    Self,
    MatchingInterface,
    ImplementedInterface,
    ExplicitService
}

internal enum RegistrationLifetime
{
    Singleton,
    Scoped,
    Transient
}

internal sealed class RegistrationModel
{
    public required INamedTypeSymbol ImplementationType { get; init; }
    public required INamedTypeSymbol? ExplicitServiceType { get; init; }
    public required RegistrationResolveBy ResolveBy { get; init; }
    public required RegistrationLifetime Lifetime { get; init; }
    public required bool TryAdd { get; init; }
    public required bool Enumerable { get; init; }
    public required string Key { get; init; }
    public required uint Order { get; init; }
    public required Location? Location { get; init; }
}
