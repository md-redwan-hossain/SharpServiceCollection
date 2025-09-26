using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Extensions;

public interface IServiceMetadata : IServiceLifetime
{
    public bool Replace { get; }
    public string Key { get; }
}