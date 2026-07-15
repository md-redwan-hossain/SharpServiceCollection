namespace SharpServiceCollection.InternalTypes;

internal readonly record struct RootRegistrationCall
{
    public required string Call { get; init; }
    public required int Priority { get; init; }
    public required string SortKey { get; init; }
}
