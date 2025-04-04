using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData.AnyInterfaceResolve;

[TryResolveByImplementedInterface(InstanceLifetime.Scoped)]
public class FooBarBazWithTry : IFoo, IBar, IBaz, IXyz
{
}