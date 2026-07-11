using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Generators.Constants;
using SharpServiceCollection.Interfaces;

namespace SharpServiceCollection.Generators;

[Generator]
public sealed class ServiceRegistrationGenerator : IIncrementalGenerator
{
    private const string InterfaceName = nameof(IServiceRegistration);

    private const string AttributeName = nameof(ServiceRegistrationItemAttribute);
    private const string AttributeMetadataName = "SharpServiceCollection.Attributes." + AttributeName;
    private const string GeneratedFileName = "SharpServiceCollection.ServiceRegistration.g.cs";
    private const string OrderPropertyName = "Order";
    private const string RootPropertyName = "ServiceRegistrationRoot";
    private const string GeneratedMethodName = "ExecuteServiceRegistrationItemsAsync";

    private const string ServiceCollectionType =
        "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";


    private const string ServiceRegistrationMustBeSealedTitle = "Annotated class is not sealed";

    private const string ServiceRegistrationMustBeSealedDescription =
        "Types annotated with [ServiceRegistrationItem] must be sealed.";

    private const string ServiceRegistrationItemMustImplementInterfaceTitle =
        "Missing implementation of IServiceRegistration";

    private const string ServiceRegistrationMustImplementInterfaceDescription =
        "Types annotated with [ServiceRegistrationItem] must implement IServiceRegistration or IServiceRegistration<TContext>.";

    private static readonly DiagnosticDescriptor MustBeSealed = new(
        id: "SSC006",
        title: ServiceRegistrationMustBeSealedTitle,
        messageFormat: "Type '{0}' is decorated with [ServiceRegistrationItem] but is not sealed",
        category: SharedConsts.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ServiceRegistrationMustBeSealedDescription,
        helpLinkUri: string.Format(SharedConsts.HelpLinkUriFormat, "service-registration"));

    private static readonly DiagnosticDescriptor MustImplementInterface = new(
        id: "SSC007",
        title: ServiceRegistrationItemMustImplementInterfaceTitle,
        messageFormat: "Type '{0}' must implement IServiceRegistration or IServiceRegistration<TContext>",
        category: SharedConsts.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ServiceRegistrationMustImplementInterfaceDescription,
        helpLinkUri: string.Format(SharedConsts.HelpLinkUriFormat, "service-registration"));

    private readonly record struct RegistrationDescriptor(
        string ImplementationTypeName,
        ITypeSymbol? ContextType,
        string? ContextTypeName,
        uint Order);

    private readonly record struct RegistrationAnalysis(
        ImmutableArray<RegistrationDescriptor> Descriptors,
        ImmutableArray<Diagnostic> Diagnostics)
    {
        public bool HasDescriptors => !Descriptors.IsDefaultOrEmpty;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var analyzedCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeMetadataName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => AnalyzeClass(ctx));

        // Diagnostics are emitted per candidate. A change in one class does not
        // force validation of every other annotated class.
        context.RegisterSourceOutput(
            analyzedCandidates,
            static (spc, analysis) => ReportDiagnostics(spc, analysis));

        // Only the root project needs the aggregate source file. The analyzer
        // config provider avoids combining the pipeline with the full Compilation.
        var isRoot = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
                provider.GlobalOptions.TryGetValue(
                    "build_property." + RootPropertyName,
                    out var value) &&
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        var generation = analyzedCandidates
            .Collect()
            .Combine(isRoot);

        context.RegisterSourceOutput(
            generation,
            static (spc, input) =>
            {
                if (!input.Right || input.Left.IsDefaultOrEmpty)
                {
                    return;
                }

                GenerateSource(spc, input.Left);
            });
    }

    private static RegistrationAnalysis AnalyzeClass(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return default;
        }

        var diagnostics = ImmutableArray<Diagnostic>.Empty;
        var location = classSymbol.Locations.Length == 0
            ? null
            : classSymbol.Locations[0];

        var isSealed = classSymbol.IsSealed;
        var interfaces = classSymbol.AllInterfaces;
        var hasServiceRegistration = false;

        foreach (var implementedInterface in interfaces)
        {
            if (IsServiceRegistrationInterface(implementedInterface))
            {
                hasServiceRegistration = true;
                break;
            }
        }

        if (!isSealed || !hasServiceRegistration)
        {
            var diagnosticBuilder = ImmutableArray.CreateBuilder<Diagnostic>(2);

            if (!isSealed)
            {
                diagnosticBuilder.Add(Diagnostic.Create(
                    MustBeSealed,
                    location,
                    classSymbol.Name));
            }

            if (!hasServiceRegistration)
            {
                diagnosticBuilder.Add(Diagnostic.Create(
                    MustImplementInterface,
                    location,
                    classSymbol.Name));
            }

            diagnostics = diagnosticBuilder.ToImmutable();

            // Invalid classes are diagnosed but never emitted into generated code.
            return new RegistrationAnalysis([], diagnostics);
        }

        var implementationTypeName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var order = GetOrder(classSymbol);
        var descriptorBuilder = ImmutableArray.CreateBuilder<RegistrationDescriptor>(2);

        foreach (var implementedInterface in interfaces)
        {
            if (!IsServiceRegistrationInterface(implementedInterface))
            {
                continue;
            }

            var contextType = implementedInterface.Arity == 1
                ? implementedInterface.TypeArguments[0]
                : null;

            descriptorBuilder.Add(new RegistrationDescriptor(
                implementationTypeName,
                contextType,
                contextType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                order));
        }

        return new RegistrationAnalysis(descriptorBuilder.ToImmutable(), diagnostics);
    }

    private static bool IsServiceRegistrationInterface(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol.Name != InterfaceName ||
            interfaceSymbol.Arity is not (0 or 1))
        {
            return false;
        }

        var interfacesNamespace = interfaceSymbol.ContainingNamespace;
        var libraryNamespace = interfacesNamespace.ContainingNamespace;

        return interfacesNamespace.Name == "Interfaces" &&
               libraryNamespace.Name == "SharpServiceCollection" &&
               libraryNamespace.ContainingNamespace.IsGlobalNamespace;
    }

    private static uint GetOrder(INamedTypeSymbol classSymbol)
    {
        foreach (var attribute in classSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat) !=
                "global::" + AttributeMetadataName)
            {
                continue;
            }

            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key != OrderPropertyName)
                {
                    continue;
                }

                return argument.Value.Value switch
                {
                    uint value => value,
                    int value when value >= 0 => (uint)value,
                    _ => 0
                };
            }

            break;
        }

        return 0;
    }

    private static void ReportDiagnostics(
        SourceProductionContext context,
        RegistrationAnalysis analysis)
    {
        foreach (var diagnostic in analysis.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void GenerateSource(
        SourceProductionContext context,
        ImmutableArray<RegistrationAnalysis> analyses)
    {
        var nonGeneric = new List<RegistrationDescriptor>();
        var genericGroups = new Dictionary<ITypeSymbol, List<RegistrationDescriptor>>(
            SymbolEqualityComparer.Default);

        foreach (var analysis in analyses)
        {
            if (!analysis.HasDescriptors)
            {
                continue;
            }

            foreach (var descriptor in analysis.Descriptors)
            {
                if (descriptor.ContextType is null)
                {
                    nonGeneric.Add(descriptor);
                    continue;
                }

                if (!genericGroups.TryGetValue(descriptor.ContextType, out var registrations))
                {
                    registrations = new List<RegistrationDescriptor>();
                    genericGroups.Add(descriptor.ContextType, registrations);
                }

                registrations.Add(descriptor);
            }
        }

        if (nonGeneric.Count == 0 && genericGroups.Count == 0)
        {
            return;
        }

        nonGeneric.Sort(CompareDescriptors);
        foreach (var registrations in genericGroups.Values)
        {
            registrations.Sort(CompareDescriptors);
        }

        var source = BuildSource(nonGeneric, genericGroups);
        context.AddSource(GeneratedFileName, source);
    }

    private static int CompareDescriptors(
        RegistrationDescriptor left,
        RegistrationDescriptor right)
    {
        var orderComparison = left.Order.CompareTo(right.Order);

        return orderComparison != 0
            ? orderComparison
            : StringComparer.Ordinal.Compare(
                left.ImplementationTypeName,
                right.ImplementationTypeName);
    }

    private static string BuildSource(
        IReadOnlyList<RegistrationDescriptor> nonGeneric,
        IReadOnlyDictionary<ITypeSymbol, List<RegistrationDescriptor>> genericGroups)
    {
        var builder = new StringBuilder(1024 + ((nonGeneric.Count + genericGroups.Count) * 160));

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine();
        builder.AppendLine("namespace SharpServiceCollection.Generated;");
        builder.AppendLine();
        builder.AppendLine("public static class GeneratedServiceRegistrationExtensions");
        builder.AppendLine("{");

        AppendNonGenericMethod(builder, nonGeneric);

        var orderedGroups = new List<KeyValuePair<ITypeSymbol, List<RegistrationDescriptor>>>(genericGroups);
        orderedGroups.Sort(static (left, right) => StringComparer.Ordinal.Compare(
            left.Value[0].ContextTypeName,
            right.Value[0].ContextTypeName));

        foreach (var group in orderedGroups)
        {
            var contextTypeName = group.Value[0].ContextTypeName;
            if (contextTypeName is null)
            {
                continue;
            }

            AppendGenericMethod(builder, contextTypeName, group.Value);
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendNonGenericMethod(
        StringBuilder builder,
        IReadOnlyList<RegistrationDescriptor> registrations)
    {
        builder.Append("    public static async Task<")
            .Append(ServiceCollectionType)
            .Append("> ")
            .Append(GeneratedMethodName)
            .AppendLine("(");
        builder.Append("        this ")
            .Append(ServiceCollectionType)
            .AppendLine(" services)");
        builder.AppendLine("    {");

        foreach (var registration in registrations)
        {
            builder.Append("        await new ")
                .Append(registration.ImplementationTypeName)
                .AppendLine("().RegisterAsync(services);");
        }

        builder.AppendLine("        return services;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendGenericMethod(
        StringBuilder builder,
        string contextTypeName,
        IReadOnlyList<RegistrationDescriptor> registrations)
    {
        builder.Append("    public static async Task<")
            .Append(ServiceCollectionType)
            .Append("> ")
            .Append(GeneratedMethodName)
            .AppendLine("(");
        builder.Append("        this ")
            .Append(ServiceCollectionType)
            .AppendLine(" services,");
        builder.Append("        ")
            .Append(contextTypeName)
            .AppendLine(" context)");
        builder.AppendLine("    {");

        foreach (var registration in registrations)
        {
            builder.Append("        await new ")
                .Append(registration.ImplementationTypeName)
                .AppendLine("().RegisterAsync(services, context);");
        }

        builder.AppendLine("        return services;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }
}