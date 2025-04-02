using System;

namespace SharpServiceCollection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TryResolveFromAttribute<T> : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public TryResolveFromAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}