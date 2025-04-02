using System;

namespace SharpServiceCollection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TryResolveFromWithKeyAttribute<T> : Attribute
{
    public InstanceLifetime Lifetime { get; }
    public string Key { get; }

    public TryResolveFromWithKeyAttribute(InstanceLifetime lifetime, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        Lifetime = lifetime;
        Key = key;
    }
}