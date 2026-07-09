using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData.Interfaces;

[InjectableDependency<IScopedDemoService>(InstanceLifetime.Scoped, TryAdd = false)]
[InjectableDependency<IKeyedScopedDemoService>(InstanceLifetime.Scoped, Key = "keyed", TryAdd = false)]
public class ScopedDemoType : IScopedDemoService, IKeyedScopedDemoService
{
}
