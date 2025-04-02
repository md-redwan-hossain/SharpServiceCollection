using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData.WithConventionalName;

[ResolveByMatchingInterface(InstanceLifetime.Scoped)]
[ResolveBySelf(InstanceLifetime.Scoped)]
public class ScopedDependency : IScopedDependency
{
}