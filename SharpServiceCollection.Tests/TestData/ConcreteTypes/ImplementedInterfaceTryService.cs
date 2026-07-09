using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Tests.TestData.Interfaces;

namespace SharpServiceCollection.Tests.TestData.ConcreteTypes;

[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface)]
public class ImplementedInterfaceTryService : IResolvableMarkerAlpha, IResolvableMarkerBeta, IResolvableMarkerGamma, IResolvableMarkerDelta;
