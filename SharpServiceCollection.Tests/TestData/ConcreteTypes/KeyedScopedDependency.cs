using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency<IScopedKeyedDependency>(InstanceLifetime.Scoped, TryAdd = false)]
[InjectableDependency<IKeyedScopedDependency>(InstanceLifetime.Scoped, Key = "keyed", TryAdd = false)]
public class KeyedScopedDependency : IScopedKeyedDependency, IKeyedScopedDependency;
