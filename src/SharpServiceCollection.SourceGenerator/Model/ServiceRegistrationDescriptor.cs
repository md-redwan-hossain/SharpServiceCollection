namespace SharpServiceCollection.SourceGenerator.Model;

internal sealed class ServiceRegistrationDescriptor
{
    public string ServiceTypeName { get; }
    public string ImplementationTypeName { get; }
    public RegistrationLifetime Lifetime { get; }
    public bool TryAdd { get; }
    public bool Enumerable { get; }
    public string Key { get; }
    public uint Order { get; }
    public string ImplementationNameSortKey { get; }

    public ServiceRegistrationDescriptor(
        string serviceTypeName,
        string implementationTypeName,
        RegistrationLifetime lifetime,
        bool tryAdd,
        bool enumerable,
        string key,
        uint order,
        string implementationNameSortKey)
    {
        ServiceTypeName = serviceTypeName;
        ImplementationTypeName = implementationTypeName;
        Lifetime = lifetime;
        TryAdd = tryAdd;
        Enumerable = enumerable;
        Key = key;
        Order = order;
        ImplementationNameSortKey = implementationNameSortKey;
    }
}
