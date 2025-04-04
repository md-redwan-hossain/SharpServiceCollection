using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ResolveByImplementedInterfaceAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public ResolveByImplementedInterfaceAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}