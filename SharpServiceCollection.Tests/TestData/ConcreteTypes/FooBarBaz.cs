using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface, TryAdd = false)]
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface)]
public class FooBarBaz : IFoo, IBar, IBaz
{
}
