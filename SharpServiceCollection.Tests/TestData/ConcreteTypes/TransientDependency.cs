using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[ResolveByMatchingInterface(InstanceLifetime.Transient)]
[ResolveBySelf(InstanceLifetime.Transient)]
public class TransientDependency : ITransientDependency
{
}