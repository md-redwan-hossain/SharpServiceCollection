using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TryResolveByAttribute<T> : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public TryResolveByAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}