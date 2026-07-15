namespace SharpServiceCollection.InternalTypes;

internal readonly record struct AggregatorMethod
{
    public required string AggregatorTypeName { get; init; }
    public required string MethodName { get; init; }
    public required string? ContextTypeName { get; init; }
    public required int Priority { get; init; }
    public required string SortKey { get; init; }
}
