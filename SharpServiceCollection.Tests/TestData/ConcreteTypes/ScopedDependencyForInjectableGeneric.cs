using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency<IScopedDependencyForInjectableGeneric>(InstanceLifetime.Scoped)]
[InjectableDependency<IScopedDependencyForInjectableGeneric>(InstanceLifetime.Scoped, Key = "key")]
public class ScopedDependencyForInjectableGeneric : IScopedDependencyForInjectableGeneric
{
}