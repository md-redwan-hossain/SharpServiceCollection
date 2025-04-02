using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ResolveBySelfAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public ResolveBySelfAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}