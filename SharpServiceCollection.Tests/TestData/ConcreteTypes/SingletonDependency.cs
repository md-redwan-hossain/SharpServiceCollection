using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[ResolveByMatchingInterface(InstanceLifetime.Singleton)]
[ResolveBySelf(InstanceLifetime.Singleton)]
public class SingletonDependency : ISingletonDependency
{
}