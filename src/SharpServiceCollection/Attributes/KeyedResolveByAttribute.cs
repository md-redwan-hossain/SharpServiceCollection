using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

/// <param name="lifetime">Specifies the lifetime of the instance to be resolved.</param>
/// <param name="key">A unique string key used to resolve the instance. Must not be null or empty.</param>
/// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is null or empty.</exception>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class KeyedResolveByAttribute<T> : Attribute
{
    public InstanceLifetime Lifetime { get; }
    public string Key { get; }

    public KeyedResolveByAttribute(InstanceLifetime lifetime, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        Lifetime = lifetime;
        Key = key;
    }
}