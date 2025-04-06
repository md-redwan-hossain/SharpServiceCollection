using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

/// <param name="lifetime">
/// Specifies the lifetime of the instance to be resolved.
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public class ResolveByImplementedInterfaceAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public ResolveByImplementedInterfaceAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}