using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency(InstanceLifetime.Singleton, ResolveBy.MatchingInterface, TryAdd = false)]
[InjectableDependency(InstanceLifetime.Singleton, ResolveBy.Self, TryAdd = false)]
public class SingletonDependency : ISingletonDependency
{
}
