using System;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InjectableDependencyByAttribute<T> : Attribute, IServiceLifetime, IServiceKey, IReplaceService
{
    public InstanceLifetime Lifetime { get; }
    public bool Replace { get; }
    public string Key { get; }

    /// <param name="lifetime">
    /// Specifies the lifetime of the instance to be resolved.
    /// </param>
    public InjectableDependencyByAttribute(InstanceLifetime lifetime)
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
    public InjectableDependencyByAttribute(InstanceLifetime lifetime, bool replace)
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
    /// <param name="key">A unique string key used to resolve the instance. Must not be null or empty.</param>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="key"/> is null or empty.</exception>
    public InjectableDependencyByAttribute(InstanceLifetime lifetime, bool replace, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        Lifetime = lifetime;
        Replace = replace;
        Key = key;
    }
}