using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

namespace TrialWebApi;

[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface)]
public class TrialService : ITrialService
{
}