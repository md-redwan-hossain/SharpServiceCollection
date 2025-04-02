using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData;

[ResolveByMatchingInterface(InstanceLifetime.Singleton)]
[ResolveBySelf(InstanceLifetime.Singleton)]
public class SingletonDependency : ISingletonDependency
{
}