namespace SharpServiceCollection.Interfaces.Internal;

internal interface ITryAddService
{
    public bool TryAdd { get; }

    public bool Enumerable { get; }
}