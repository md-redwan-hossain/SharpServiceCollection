using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency<ITryResolver>(InstanceLifetime.Scoped)]
[InjectableDependency<IResolver>(InstanceLifetime.Scoped, TryAdd = false)]
public class Foo : ITryResolver, IResolver
{
}
