using Microsoft.CodeAnalysis;

namespace SharpServiceCollection.SourceGenerator.Model;

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
    public INamedTypeSymbol ImplementationType { get; }
    public INamedTypeSymbol? ExplicitServiceType { get; }
    public RegistrationResolveBy ResolveBy { get; }
    public RegistrationLifetime Lifetime { get; }
    public bool TryAdd { get; }
    public bool Enumerable { get; }
    public string Key { get; }
    public uint Order { get; }
    public Location? Location { get; }

    public RegistrationModel(
        INamedTypeSymbol implementationType,
        INamedTypeSymbol? explicitServiceType,
        RegistrationResolveBy resolveBy,
        RegistrationLifetime lifetime,
        bool tryAdd,
        bool enumerable,
        string key,
        uint order,
        Location? location)
    {
        ImplementationType = implementationType;
        ExplicitServiceType = explicitServiceType;
        ResolveBy = resolveBy;
        Lifetime = lifetime;
        TryAdd = tryAdd;
        Enumerable = enumerable;
        Key = key;
        Order = order;
        Location = location;
    }
}
