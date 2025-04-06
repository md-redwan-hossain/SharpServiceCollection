using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

/// <param name="lifetime">
/// Specifies the lifetime of the instance to be resolved.
/// </param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ResolveByAttribute<T> : Attribute
{
    public InstanceLifetime Lifetime { get; }

    public ResolveByAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}