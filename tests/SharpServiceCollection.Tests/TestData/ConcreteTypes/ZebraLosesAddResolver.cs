using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency<IOrderAddResolver>(InstanceLifetime.Scoped, TryAdd = false, Priority = 2)]
public class ZebraLosesAddResolver : IOrderAddResolver;
