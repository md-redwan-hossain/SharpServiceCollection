using System;

namespace SharpServiceCollection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ResolveFromAttribute<T> : Attribute
{
    public InstanceLifetime Lifetime { get; }
    
    public ResolveFromAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}