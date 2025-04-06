using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

/// <param name="lifetime">The lifetime of the instance to be resolved. Specifies how the instance should be managed.</param>
/// <param name="key">The unique key associated with the instance to be resolved. Must not be null or empty.</param>
/// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is null or an empty string.</exception>
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