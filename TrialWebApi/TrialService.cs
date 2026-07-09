using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace TrialWebApi;

[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface, TryAdd = true)]
public class TrialService : ITrialService
{
}