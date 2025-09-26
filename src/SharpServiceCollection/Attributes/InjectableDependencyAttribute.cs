using System;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Extensions;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InjectableDependencyAttribute : Attribute, IServiceLifetime, IServiceKey, IReplaceService
{
    public InstanceLifetime Lifetime { get; }
    public ResolveBy ResolveBy { get; }
    public bool Replace { get; }
    public string Key { get; }

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
    }

    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    /// 
    /// <param name="resolveBy">
    /// Specifies the dependency resolution approach.
    /// </param>
    ///    
    /// <param name="key">A unique string key used to resolve the instance. Must not be null or empty.</param>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is null or empty.</exception>
    public InjectableDependencyAttribute(InstanceLifetime lifetime, ResolveBy resolveBy, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        Lifetime = lifetime;
        ResolveBy = resolveBy;
        Key = key;
    }
}