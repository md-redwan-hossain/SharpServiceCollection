using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

/// <param name="lifetime">
/// Specifies the lifetime of the instance to be resolved.
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public class ResolveByMatchingInterfaceAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public ResolveByMatchingInterfaceAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}