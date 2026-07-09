using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface, TryAdd = false)]
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self, TryAdd = false)]
public class ScopedDependency : IScopedDependency;
