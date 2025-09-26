using System;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InjectableDependencyByAttribute<T> : Attribute
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
    /// <param name="key">
    ///  /// Specifies the key by which the dependency will be resolved.
    /// </param>
    public InjectableDependencyByAttribute(InstanceLifetime lifetime, bool replace, string key)
    {
        Lifetime = lifetime;
        Replace = replace;
        Key = key;
    }
}