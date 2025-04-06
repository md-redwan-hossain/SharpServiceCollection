using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

/// <param name="lifetime">
/// Specifies the lifetime of the instance to be resolved.
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public class TryResolveByImplementedInterfaceAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public TryResolveByImplementedInterfaceAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}