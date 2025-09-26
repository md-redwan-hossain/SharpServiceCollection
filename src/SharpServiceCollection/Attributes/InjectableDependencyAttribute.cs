using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

public class InjectableDependencyAttribute : Attribute
{
    public InstanceLifetime Lifetime { get; }
    public ResolveBy ResolveBy { get; }
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
    /// <param name="key">
    ///  /// Specifies the key by which the dependency will be resolved.
    /// </param>
    public InjectableDependencyAttribute(InstanceLifetime lifetime, ResolveBy resolveBy, string key)
    {
        Lifetime = lifetime;
        ResolveBy = resolveBy;
        Key = key;
    }
}

public class InjectableDependencyAttribute<T> : Attribute
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
    /// <param name="replace">
    ///  Indicates whether Try pattern will be used or not.
    /// </param>
    /// 
    /// <param name="key">
    ///  /// Specifies the key by which the dependency will be resolved.
    /// </param>
    public InjectableDependencyAttribute(InstanceLifetime lifetime, bool replace, string key)
    {
        Lifetime = lifetime;
        Replace = replace;
        Key = key;
    }
}