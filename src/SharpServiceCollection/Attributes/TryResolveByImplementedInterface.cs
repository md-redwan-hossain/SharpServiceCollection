using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TryResolveByImplementedInterfaceAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public TryResolveByImplementedInterfaceAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}