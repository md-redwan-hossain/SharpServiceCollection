using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency(InstanceLifetime.Transient, ResolveBy.MatchingInterface, TryAdd = false)]
[InjectableDependency(InstanceLifetime.Transient, ResolveBy.Self, TryAdd = false)]
public class TransientDependency : ITransientDependency;
