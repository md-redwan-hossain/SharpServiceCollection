using System;

namespace SharpServiceCollection.Attributes;

/// <summary>
/// Marks the current assembly as the root of a service-registration
/// composition graph. Only assemblies carrying this attribute emit the
/// generated aggregator extension method, which prevents leaf assemblies
/// from redeclaring the same entry point and avoids duplicate registrations
/// at startup.
/// </summary>
/// <remarks>
/// Place the attribute in any file inside the project, e.g.
/// <c>[assembly: ServiceRegistrationRoot]</c>. The attribute itself has no
/// members; its presence is the signal. Source generators see this directly
/// via <c>Compilation.Assembly.GetAttributes()</c>, so NuGet consumers get
/// it automatically with no <c>CompilerVisibleProperty</c> ceremony in the
/// consuming csproj.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ServiceRegistrationRootAttribute : Attribute
{
}