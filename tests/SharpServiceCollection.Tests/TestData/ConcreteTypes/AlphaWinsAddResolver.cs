using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency<IOrderAddResolver>(InstanceLifetime.Scoped, TryAdd = false, Priority = 1)]
public class AlphaWinsAddResolver : IOrderAddResolver;
