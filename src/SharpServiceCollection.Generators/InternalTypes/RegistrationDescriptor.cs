using Microsoft.CodeAnalysis;

namespace SharpServiceCollection.InternalTypes;

internal readonly record struct RegistrationDescriptor
{
    public required string ImplementationTypeName { get; init; }
    public required ITypeSymbol? ContextType { get; init; }
    public required string? ContextTypeName { get; init; }
    public required uint Order { get; init; }
}
