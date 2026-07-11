namespace SharpServiceCollection.InternalTypes;

internal readonly record struct RootRegistrationCall
{
    public required string Call { get; init; }
    public required uint Order { get; init; }
    public required string SortKey { get; init; }
}
