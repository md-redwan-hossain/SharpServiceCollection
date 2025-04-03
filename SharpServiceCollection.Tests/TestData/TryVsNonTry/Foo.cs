using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection.Tests.TestData.TryVsNonTry;

[TryResolveBy<ITryResolver>(InstanceLifetime.Scoped)]
[ResolveBy<IResolver>(InstanceLifetime.Scoped)]
public class Foo : ITryResolver, IResolver
{
}