using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self)]
[InjectableDependency(InstanceLifetime.Transient, ResolveBy.ImplementedInterface, Key = "key-111")]
public class Demo : IDemo
{
}
