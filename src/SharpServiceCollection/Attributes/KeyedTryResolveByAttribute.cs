using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class KeyedTryResolveByAttribute<T> : Attribute
{
    public InstanceLifetime Lifetime { get; }
    public string Key { get; }

    public KeyedTryResolveByAttribute(InstanceLifetime lifetime, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        Lifetime = lifetime;
        Key = key;
    }
}