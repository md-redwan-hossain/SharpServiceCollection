using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData;

[ResolveByMatchingInterface(InstanceLifetime.Scoped)]
[ResolveBySelf(InstanceLifetime.Scoped)]
public class ScopedDependency : IScopedDependency
{
}