using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData.Interfaces;

[ResolveBy<IScopedDemoService>(InstanceLifetime.Scoped)]
[KeyedResolveBy<IKeyedScopedDemoService>(InstanceLifetime.Scoped, "keyed")]
public class ScopedDemoType : IScopedDemoService, IKeyedScopedDemoService
{
}