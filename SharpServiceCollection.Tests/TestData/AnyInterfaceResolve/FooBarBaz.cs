using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData.AnyInterfaceResolve;

[ResolveByImplementedInterface(InstanceLifetime.Scoped)]
[TryResolveByImplementedInterface(InstanceLifetime.Scoped)]
public class FooBarBaz : IFoo, IBar, IBaz
{
}