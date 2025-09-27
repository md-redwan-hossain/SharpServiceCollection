using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[TryResolveBySelf(InstanceLifetime.Scoped)]
[InjectableDependency(InstanceLifetime.Transient, ResolveBy.ImplementedInterface, "key-111")]
public class Demo : IDemo
{
}