using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TryResolveBySelfAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public TryResolveBySelfAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}