using System;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Attributes;

/// <summary>
/// Marks a class as a service-registration aggregator. The source generator
/// discovers every sealed, attributed class in the project graph and emits
/// a single extension method on <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>
/// that instantiates them in <see cref="Order"/> ascending order.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ServiceRegistrationAttribute : Attribute, IServiceOrder
{
    /// <summary>
    /// Sort order for aggregators. Lower numbers run first. Aggregators with
    /// the same <see cref="Order"/> are then sorted by their fully qualified
    /// type name to keep the emit deterministic. Defaults to <c>0</c>.
    /// </summary>
    public uint Order { get; set; }
}
