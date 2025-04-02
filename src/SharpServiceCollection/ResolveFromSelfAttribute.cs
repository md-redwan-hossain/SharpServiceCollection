using System;

namespace SharpServiceCollection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ResolveFromSelfAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }
    public bool UseTryPattern { get; }

    public ResolveFromSelfAttribute(InstanceLifetime lifetime, bool useTryPattern = true)
    {
        Lifetime = lifetime;
        UseTryPattern = useTryPattern;
    }
}