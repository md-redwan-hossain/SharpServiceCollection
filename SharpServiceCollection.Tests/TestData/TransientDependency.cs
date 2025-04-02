using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData;

[ResolveByMatchingInterface(InstanceLifetime.Transient)]
[ResolveBySelf(InstanceLifetime.Transient)]
public class TransientDependency : ITransientDependency
{
}