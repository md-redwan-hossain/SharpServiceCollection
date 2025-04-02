using System;

namespace SharpServiceCollection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ResolveFromWithKeyAttribute<T> : Attribute
{
    public InstanceLifetime Lifetime { get; }
    public string Key { get; }

    public ResolveFromWithKeyAttribute(InstanceLifetime lifetime, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        Lifetime = lifetime;
        Key = key;
    }
}