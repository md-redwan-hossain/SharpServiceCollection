using System;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TryResolveByMatchingInterfaceAttribute : Attribute, IServiceLifetime
{
    public InstanceLifetime Lifetime { get; }

    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    public TryResolveByMatchingInterfaceAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}