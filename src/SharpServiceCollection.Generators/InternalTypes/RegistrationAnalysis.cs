using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SharpServiceCollection.InternalTypes;

internal readonly record struct RegistrationAnalysis
{
    public required ImmutableArray<RegistrationDescriptor> Descriptors { get; init; }
    public required ImmutableArray<Diagnostic> Diagnostics { get; init; }

    public bool HasDescriptors => !Descriptors.IsDefaultOrEmpty;
}
