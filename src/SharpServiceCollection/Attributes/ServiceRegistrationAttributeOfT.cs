using System;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Attributes;

/// <summary>
/// Generic variant of <see cref="ServiceRegistrationAttribute"/>. Aggregates
/// that need application context (e.g. <c>IConfiguration</c>,
/// <c>IHostEnvironment</c>) bind <c>T</c> here; the host calls the matching
/// overload of <c>ExecuteServiceRegistrationsAsync(context)</c>.
/// </summary>
/// <typeparam name="T">Application-defined context type passed to <c>ExecuteAsync</c>.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ServiceRegistrationAttribute<T> : Attribute, IServiceOrder
{
    /// <summary>
    /// Sort order for aggregators. Lower numbers run first. Aggregators with
    /// the same <see cref="Order"/> are then sorted by their fully qualified
    /// type name to keep the emit deterministic. Defaults to <c>0</c>.
    /// </summary>
    public uint Order { get; set; }
}