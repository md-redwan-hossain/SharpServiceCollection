using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ResolveByAttribute<T> : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public ResolveByAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}