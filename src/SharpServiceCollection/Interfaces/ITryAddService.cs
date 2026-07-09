namespace SharpServiceCollection.Interfaces;

internal interface ITryAddService
{
    public bool TryAdd { get; }

    public bool Enumerable { get; }
}