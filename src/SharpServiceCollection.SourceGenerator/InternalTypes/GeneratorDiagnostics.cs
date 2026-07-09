using Microsoft.CodeAnalysis;

namespace SharpServiceCollection.SourceGenerator.InternalTypes;

internal static class GeneratorDiagnostics
{
    private const string EnumerableRequiresTryAddTitle = "Enumerable=true requires TryAdd=true";
    private const string MatchingInterfaceMissingTitle = "Matching interface not found";
    private const string InvalidLifetimeTitle = "Unsupported lifetime value";
    private const string InvalidResolveByTitle = "Unsupported resolve-by value";

    internal static readonly DiagnosticDescriptor EnumerableRequiresTryAdd = new(
        id: "SSC001",
        title: EnumerableRequiresTryAddTitle,
        messageFormat: $"{EnumerableRequiresTryAddTitle} for '{{0}}'",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MatchingInterfaceMissing = new(
        id: "SSC002",
        title: MatchingInterfaceMissingTitle,
        messageFormat: "ResolveBy.MatchingInterface requires interface '{0}' on '{1}'",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor InvalidLifetime = new(
        id: "SSC003",
        title: InvalidLifetimeTitle,
        messageFormat: $"{InvalidLifetimeTitle} '{{0}}' on '{{1}}'",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor InvalidResolveBy = new(
        id: "SSC004",
        title: InvalidResolveByTitle,
        messageFormat: "Unsupported resolve strategy on '{0}'",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
