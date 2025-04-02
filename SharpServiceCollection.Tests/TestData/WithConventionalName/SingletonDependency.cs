using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData.WithConventionalName;

[ResolveByMatchingInterface(InstanceLifetime.Singleton)]
[ResolveBySelf(InstanceLifetime.Singleton)]
public class SingletonDependency : ISingletonDependency
{
}