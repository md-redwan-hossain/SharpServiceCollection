using System;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Extensions;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InjectableDependencyAttribute : Attribute, IServiceMetadata
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