using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency<IOrderTryResolver>(InstanceLifetime.Scoped, Priority = 2)]
public class ZebraWinsTryAddResolver : IOrderTryResolver;
