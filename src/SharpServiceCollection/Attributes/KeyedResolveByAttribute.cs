using System;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class KeyedResolveByAttribute<T> : Attribute, IServiceLifetime, IServiceKey
{
    public InstanceLifetime Lifetime { get; }
    public string Key { get; }

    /// <param name="lifetime">Specifies the lifetime of the instance to be resolved.</param>
    /// <param name="key">A unique string key used to resolve the instance. Must not be null or empty.</param>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is null or empty.</exception>
    public KeyedResolveByAttribute(InstanceLifetime lifetime, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        Lifetime = lifetime;
        Key = key;
    }
}