using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData.WithConventionalName;

[ResolveByMatchingInterface(InstanceLifetime.Transient)]
[ResolveBySelf(InstanceLifetime.Transient)]
public class TransientDependency : ITransientDependency
{
}