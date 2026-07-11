using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Generators.Constants;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Generators;

[Generator]
public class ServiceRegistrationGenerator : IIncrementalGenerator
{
    private const string ServiceRegistrationMustBeSealedTitle = "Annoted class is not sealed";

    private const string ServiceRegistrationMustBeSealedDescription =
        "Types annotated with [ServiceRegistration] must be sealed.";

    private const string ServiceRegistrationItemMustImplementInterfaceTitle =
        "Missing implementation of IServiceRegistration";

    private const string ServiceRegistrationMustImplementExecuteAsyncDescription =
        "Types annotated with [ServiceRegistrationItem] must implement the IServiceRegistration interface.";

    private static readonly DiagnosticDescriptor MustBeSealed = new(
        id: "SSC006",
        title: ServiceRegistrationMustBeSealedTitle,
        messageFormat: "Type '{0}' is decorated with [ServiceRegistration] but is not sealed",
        category: SharedConsts.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ServiceRegistrationMustBeSealedDescription,
        helpLinkUri: string.Format(SharedConsts.HelpLinkUriFormat, "service-registration"));

    private static readonly DiagnosticDescriptor MustImplementInterface = new(
        id: "SSC007",
        title: ServiceRegistrationItemMustImplementInterfaceTitle,
        messageFormat: "Type '{0}' must implement IServiceRegistration",
        category: SharedConsts.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ServiceRegistrationMustImplementExecuteAsyncDescription,
        helpLinkUri: string.Format(SharedConsts.HelpLinkUriFormat, "service-registration"));


    private const string InterfaceName = nameof(IServiceRegistration);
    private const string GeneratedFileName = "SharpServiceCollection.ServiceRegistration.g.cs";
    private const string InterfaceNamespace = "SharpServiceCollection.Interfaces";
    private const string AttributeNamespace = "SharpServiceCollection.Attributes";
    private const string AttributeName = nameof(ServiceRegistrationItemAttribute);
    private const string FullyQualifiedAttributeName = $"{AttributeNamespace}.{AttributeName}";
    private const string Indent = "        ";


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidateClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: FullyQualifiedAttributeName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetValidClassSymbol(ctx))
            .Where(static symbol => symbol is not null);

        context.RegisterSourceOutput(candidateClasses.Collect(), static (spc, classes) =>
        {
            foreach (var classSymbol in classes)
            {
                if (classSymbol is not null)
                {
                    ValidateServiceRegistrationClass(spc, classSymbol);
                }
            }
        });
    }

    private static INamedTypeSymbol? GetValidClassSymbol(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol namedType)
        {
            return null;
        }

        return namedType.TypeKind != TypeKind.Class ? null : namedType;
    }

    private static void ValidateServiceRegistrationClass(SourceProductionContext spc, INamedTypeSymbol classSymbol)
    {
        var location = classSymbol.Locations.FirstOrDefault();

        if (!classSymbol.IsSealed)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                MustBeSealed,
                location,
                classSymbol.Name));
        }

        var implementsNonGeneric = classSymbol.AllInterfaces
            .Any(i => i.Name == InterfaceName
                      && i.ContainingNamespace is not null
                      && i.ContainingNamespace.ToDisplayString() == InterfaceNamespace);

        var implementsGeneric = classSymbol.AllInterfaces.Any(i =>
            i.Arity == 1 &&
            i.Name == InterfaceName &&
            i.ContainingNamespace?.ToDisplayString() == InterfaceNamespace);

        if (!implementsNonGeneric && !implementsGeneric)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                MustImplementInterface,
                location,
                classSymbol.Name));
        }
    }
}