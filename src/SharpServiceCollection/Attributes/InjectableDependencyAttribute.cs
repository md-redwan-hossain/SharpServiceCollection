using System;
using SharpServiceCollection.Enums;
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


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InjectableDependencyAttribute<T> : Attribute, IServiceLifetime, IServiceKey, IReplaceService
{
    public InstanceLifetime Lifetime { get; }
    public bool Replace { get; }
    public string Key { get; }

    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    public InjectableDependencyAttribute(InstanceLifetime lifetime)
    {
        Lifetime = lifetime;
        Replace = false;
        Key = string.Empty;
    }

    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    /// 
    /// <param name="replace">
    ///  Indicates whether Try pattern will be used or not.
    /// </param>
    public InjectableDependencyAttribute(InstanceLifetime lifetime, bool replace)
    {
        Lifetime = lifetime;
        Replace = replace;
        Key = string.Empty;
    }

    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    /// 
    /// <param name="key">A unique string key used to resolve the instance. Must not be null or empty.</param>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is null or empty.</exception>
    public InjectableDependencyAttribute(InstanceLifetime lifetime, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        Lifetime = lifetime;
        Replace = false;
        Key = key;
    }


    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    /// 
    /// <param name="replace">
    ///  Indicates whether Try pattern will be used or not.
    /// </param>
    /// 
    /// <param name="key">A unique string key used to resolve the instance. Must not be null or empty.</param>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is null or empty.</exception>
    public InjectableDependencyAttribute(InstanceLifetime lifetime, bool replace, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        Lifetime = lifetime;
        Replace = replace;
        Key = key;
    }
}