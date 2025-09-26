using System;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TryResolveBySelfAttribute : Attribute, IServiceLifetime
{
    public InstanceLifetime Lifetime { get; }

    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    public TryResolveBySelfAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}