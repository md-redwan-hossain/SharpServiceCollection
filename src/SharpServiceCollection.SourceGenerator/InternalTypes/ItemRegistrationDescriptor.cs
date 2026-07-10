namespace SharpServiceCollection.SourceGenerator.InternalTypes;

internal sealed class ItemRegistrationDescriptor
{
    public required string FullyQualifiedTypeName { get; init; }
    public required string? ContextTypeName { get; init; }
    public required bool IsGeneric { get; init; }
}
