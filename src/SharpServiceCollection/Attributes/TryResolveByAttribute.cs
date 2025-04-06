using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

/// <param name="lifetime">
/// Specifies the lifetime of the instance to be resolved.
/// </param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TryResolveByAttribute<T> : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public TryResolveByAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}