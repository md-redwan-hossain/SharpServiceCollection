using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependencyBy<IScopedDependencyForInjectableBy>(InstanceLifetime.Scoped)]
public class ScopedDependencyForInjectableBy : IScopedDependencyForInjectableBy
{
}