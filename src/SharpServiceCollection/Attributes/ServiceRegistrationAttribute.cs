using System;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ServiceRegistrationAttribute : Attribute, IServiceOrder
{
    public uint Order { get; set; }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ServiceRegistrationAttribute<TContext> : Attribute, IServiceOrder
{
    public uint Order { get; set; }
}