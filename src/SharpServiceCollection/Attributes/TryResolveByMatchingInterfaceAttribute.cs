using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

/// <param name="lifetime">
/// Specifies the lifetime of the instance to be resolved.
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public class TryResolveByMatchingInterfaceAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public TryResolveByMatchingInterfaceAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}