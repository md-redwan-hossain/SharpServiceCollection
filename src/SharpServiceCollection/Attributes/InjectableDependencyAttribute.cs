using System;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InjectableDependencyAttribute : Attribute, IServiceLifetime, IServiceKey, ITryAddService
{
    public InstanceLifetime Lifetime { get; }
    public ResolveBy ResolveBy { get; }
    public string Key { get; set; }
    public bool TryAdd { get; set; }

    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    /// 
    /// <param name="resolveBy">
    /// Specifies the dependency resolution approach.
    /// </param>
    ///    
    public InjectableDependencyAttribute(InstanceLifetime lifetime, ResolveBy resolveBy)
    {
        Lifetime = lifetime;
        ResolveBy = resolveBy;
        Key = string.Empty;
        TryAdd = true;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InjectableDependencyAttribute<T> : Attribute, IServiceLifetime, IServiceKey, ITryAddService
{
    public InstanceLifetime Lifetime { get; }
    public bool TryAdd { get; set; }
    public string Key { get; set; }

    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    public InjectableDependencyAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
        TryAdd = true;
        Key = string.Empty;
    }
}