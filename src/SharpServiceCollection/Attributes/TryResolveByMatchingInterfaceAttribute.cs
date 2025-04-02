using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TryResolveByMatchingInterfaceAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public TryResolveByMatchingInterfaceAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}