using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[TryResolveBy<ITryResolver>(InstanceLifetime.Scoped)]
[ResolveBy<IResolver>(InstanceLifetime.Scoped)]
public class Baz : ITryResolver, IResolver
{
}