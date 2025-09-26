using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface)]
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self)]
public class ScopedDependencyForInjectable: IScopedDependencyForInjectable   
{
    
}