using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[TryResolveByImplementedInterface(InstanceLifetime.Scoped)]
public class FooBarBazWithTry : IFoo, IBar, IBaz, IXyz
{
}