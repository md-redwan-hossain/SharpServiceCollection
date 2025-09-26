using System;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TryResolveByAttribute<T> : Attribute, IServiceLifetime
{
    public InstanceLifetime Lifetime { get; }

    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    public TryResolveByAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}