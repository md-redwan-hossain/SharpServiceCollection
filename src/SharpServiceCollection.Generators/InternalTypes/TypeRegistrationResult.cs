using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SharpServiceCollection.InternalTypes;

internal readonly record struct TypeRegistrationResult
{
    public required ImmutableArray<ServiceRegistrationDescriptor> Descriptors { get; init; }
    public required ImmutableArray<Diagnostic> Diagnostics { get; init; }
}
