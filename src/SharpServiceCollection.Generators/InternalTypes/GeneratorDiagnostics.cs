using Microsoft.CodeAnalysis;

namespace SharpServiceCollection.Generators.InternalTypes;

internal static class GeneratorDiagnostics
{
    private const string HelpLinkUriFormat = "https://github.com/md-redwan-hossain/SharpServiceCollection/blob/main/README.md#{0}";

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
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: EnumerableRequiresTryAddDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-registration-aot-friendly"));

    internal static readonly DiagnosticDescriptor MatchingInterfaceMissing = new(
        id: "SSC002",
        title: MatchingInterfaceMissingTitle,
        messageFormat: "ResolveBy.MatchingInterface requires interface '{0}' on '{1}'",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: MatchingInterfaceMissingDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-registration-aot-friendly"));

    internal static readonly DiagnosticDescriptor InvalidLifetime = new(
        id: "SSC003",
        title: InvalidLifetimeTitle,
        messageFormat: "Unsupported lifetime '{0}' on '{1}'",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: InvalidLifetimeDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-registration-aot-friendly"));

    internal static readonly DiagnosticDescriptor InvalidResolveBy = new(
        id: "SSC004",
        title: InvalidResolveByTitle,
        messageFormat: "Unsupported resolve strategy on '{0}'",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: InvalidResolveByDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-registration-aot-friendly"));

    private const string ServiceRegistrationMustBeSealedTitle = "Service registration type must be sealed";
    private const string ServiceRegistrationMustBeSealedDescription =
        "Types annotated with [ServiceRegistration] or [ServiceRegistration<T>] must be sealed.";

    private const string ServiceRegistrationMissingCtorTitle = "Service registration type requires a parameterless constructor";
    private const string ServiceRegistrationMissingCtorDescription =
        "[ServiceRegistration] / [ServiceRegistration<T>] types must have an accessible parameterless constructor so the host can instantiate them.";

    private const string ServiceRegistrationMustImplementExecuteAsyncTitle =
        "Service registration type must implement ExecuteAsync";
    private const string ServiceRegistrationMustImplementExecuteAsyncDescription =
        "[ServiceRegistration] / [ServiceRegistration<T>] types must expose an accessible ExecuteAsync(IServiceCollection[, TContext]) method.";

    internal static readonly DiagnosticDescriptor ServiceRegistrationMustBeSealed = new(
        id: "SSC006",
        title: ServiceRegistrationMustBeSealedTitle,
        messageFormat: "Type '{0}' is decorated with [ServiceRegistration] but is not sealed",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ServiceRegistrationMustBeSealedDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "service-registration"));

    internal static readonly DiagnosticDescriptor ServiceRegistrationMissingParameterlessConstructor = new(
        id: "SSC007",
        title: ServiceRegistrationMissingCtorTitle,
        messageFormat: "Type '{0}' must have an accessible parameterless constructor",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ServiceRegistrationMissingCtorDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "service-registration"));

    internal static readonly DiagnosticDescriptor ServiceRegistrationMustImplementExecuteAsync = new(
        id: "SSR008",
        title: ServiceRegistrationMustImplementExecuteAsyncTitle,
        messageFormat: "Type '{0}' must implement ExecuteAsync(IServiceCollection[, TContext])",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ServiceRegistrationMustImplementExecuteAsyncDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "service-registration"));

    // TEMPORARY: re-instrumented to catch the persistent NRE. Will be removed once the
    // offending access is identified.
    internal static readonly DiagnosticDescriptor DebugDiagnostic = new(
        id: "SSR999",
        title: "Generator debug",
        messageFormat: "{0}",
        category: GeneratorConstants.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Temporary debug diagnostic to surface generator exceptions.",
        helpLinkUri: string.Empty);
}
