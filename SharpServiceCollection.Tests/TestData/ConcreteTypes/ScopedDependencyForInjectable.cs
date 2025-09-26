using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface)]
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self)]
public class ScopedDependencyForInjectable: IScopedDependency   
{
    
}