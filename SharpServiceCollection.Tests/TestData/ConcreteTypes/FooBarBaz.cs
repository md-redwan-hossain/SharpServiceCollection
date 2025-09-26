using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[ResolveByImplementedInterface(InstanceLifetime.Scoped)]
[TryResolveByImplementedInterface(InstanceLifetime.Scoped)]
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface)]
public class FooBarBaz : IFoo, IBar, IBaz
{
}