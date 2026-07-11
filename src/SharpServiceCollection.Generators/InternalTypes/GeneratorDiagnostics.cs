using Microsoft.CodeAnalysis;
using SharpServiceCollection.Generators.Constants;

namespace SharpServiceCollection.Generators.InternalTypes;

internal static class GeneratorDiagnostics
{
    private const string HelpLinkUriFormat =
        "https://github.com/md-redwan-hossain/SharpServiceCollection/blob/main/README.md#{0}";

    private const string EnumerableRequiresTryAddTitle = "Enumerable registration requires TryAdd";

    private const string EnumerableRequiresTryAddDescription =
        "When Enumerable is true, registrations must use TryAdd so duplicate implementations can coexist.";

    private const string MatchingInterfaceMissingTitle = "Matching interface not found";

    private const string MatchingInterfaceMissingDescription =
        "ResolveBy.MatchingInterface expects a public interface named I{TypeName}.";

    private const string InvalidLifetimeTitle = "Unsupported InjectableDependency lifetime";

    private const string InvalidLifetimeDescription =
        "Lifetime must be Singleton, Scoped, or Transient.";

    private const string InvalidResolveByTitle = "Unsupported InjectableDependency resolve strategy";

    private const string InvalidResolveByDescription =
        "ResolveBy must be Self, ImplementedInterface, or MatchingInterface.";

    internal static readonly DiagnosticDescriptor EnumerableRequiresTryAdd = new(
        id: "SSC001",
        title: EnumerableRequiresTryAddTitle,
        messageFormat: "Enumerable=true requires TryAdd=true for '{0}'",
        category: InjectableDependencyConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: EnumerableRequiresTryAddDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-registration"));

    internal static readonly DiagnosticDescriptor MatchingInterfaceMissing = new(
        id: "SSC002",
        title: MatchingInterfaceMissingTitle,
        messageFormat: "ResolveBy.MatchingInterface requires interface '{0}' on '{1}'",
        category: InjectableDependencyConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: MatchingInterfaceMissingDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-registration"));

    internal static readonly DiagnosticDescriptor InvalidLifetime = new(
        id: "SSC003",
        title: InvalidLifetimeTitle,
        messageFormat: "Unsupported lifetime '{0}' on '{1}'",
        category: InjectableDependencyConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: InvalidLifetimeDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-registration"));

    internal static readonly DiagnosticDescriptor InvalidResolveBy = new(
        id: "SSC004",
        title: InvalidResolveByTitle,
        messageFormat: "Unsupported resolve strategy on '{0}'",
        category: InjectableDependencyConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: InvalidResolveByDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-registration"));
}