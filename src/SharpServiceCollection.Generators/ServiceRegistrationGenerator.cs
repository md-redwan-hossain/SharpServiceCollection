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

    private const string AggregatorAttributeMetadataName =
        "global::SharpServiceCollection.Attributes.ServiceRegistrationAggregatorAttribute";

    private const string GeneratedFileName = "SharpServiceCollection.ServiceRegistration.g.cs";
    private const string AggregatorGeneratedFileName = "SharpServiceCollection.ServiceRegistration.Aggregator.g.cs";
    private const string OrderPropertyName = "Order";
    private const string RootPropertyName = "ServiceRegistrationRoot";
    private const string GeneratedMethodName = "ExecuteServiceRegistrationItemsAsync";
    private const string RegisterMethodName = "RegisterAsync";

    private const string ServiceCollectionType =
        "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";

    private const string ServiceCollectionMetadataName =
        "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";

    private const string GeneratedNamespace = "SharpServiceCollection.Generated";
    private const string AggregatorNamePrefix = "ServiceRegistrationAggregator_";

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

    private readonly record struct AggregatorMethod(
        string AggregatorTypeName,
        string? ContextTypeName,
        string SortKey);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var analyzedCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeMetadataName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => AnalyzeClass(ctx));

        context.RegisterSourceOutput(
            analyzedCandidates,
            static (spc, analysis) => ReportDiagnostics(spc, analysis));

        var collectedCandidates = analyzedCandidates.Collect();

        // Every project emits its own small public aggregator. This makes its
        // registrations visible as metadata to a referencing root project.
        var projectSource = context.CompilationProvider.Combine(collectedCandidates);
        context.RegisterSourceOutput(
            projectSource,
            static (spc, input) => GenerateProjectAggregator(
                spc,
                input.Left.AssemblyName,
                input.Right));

        var isRoot = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
                provider.GlobalOptions.TryGetValue(
                    "build_property." + RootPropertyName,
                    out var value) &&
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        var rootSource = context.CompilationProvider
            .Combine(collectedCandidates)
            .Combine(isRoot);

        context.RegisterSourceOutput(
            rootSource,
            static (spc, input) =>
            {
                if (!input.Right)
                {
                    return;
                }

                GenerateRootExtensions(
                    spc,
                    input.Left.Left,
                    input.Left.Right);
            });
    }

    private static RegistrationAnalysis AnalyzeClass(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return default;
        }

        var location = classSymbol.Locations.Length == 0
            ? null
            : classSymbol.Locations[0];
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

        if (!classSymbol.IsSealed || !hasServiceRegistration)
        {
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>(2);

            if (!classSymbol.IsSealed)
            {
                diagnostics.Add(Diagnostic.Create(
                    MustBeSealed,
                    location,
                    classSymbol.Name));
            }

            if (!hasServiceRegistration)
            {
                diagnostics.Add(Diagnostic.Create(
                    MustImplementInterface,
                    location,
                    classSymbol.Name));
            }

            return new RegistrationAnalysis([], diagnostics.ToImmutable());
        }

        var implementationTypeName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var order = GetOrder(classSymbol);
        var descriptors = ImmutableArray.CreateBuilder<RegistrationDescriptor>(2);

        foreach (var implementedInterface in interfaces)
        {
            if (!IsServiceRegistrationInterface(implementedInterface))
            {
                continue;
            }

            var contextType = implementedInterface.Arity == 1
                ? implementedInterface.TypeArguments[0]
                : null;
            var contextTypeName = contextType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            descriptors.Add(new RegistrationDescriptor(
                implementationTypeName,
                contextType,
                contextTypeName,
                order));
        }

        return new RegistrationAnalysis(descriptors.ToImmutable(), []);
    }

    private static bool IsServiceRegistrationInterface(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol.Name != InterfaceName || interfaceSymbol.Arity is not (0 or 1))
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
                if (argument.Key == OrderPropertyName)
                {
                    return argument.Value.Value switch
                    {
                        uint value => value,
                        int value when value >= 0 => (uint)value,
                        _ => 0
                    };
                }
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

    private static void GenerateProjectAggregator(
        SourceProductionContext context,
        string? assemblyName,
        ImmutableArray<RegistrationAnalysis> analyses)
    {
        var descriptors = GetDescriptors(analyses);
        if (descriptors.Count == 0 || assemblyName is null || string.IsNullOrWhiteSpace(assemblyName))
        {
            return;
        }

        var aggregatorName = AggregatorNamePrefix + SanitizeIdentifier(assemblyName);
        var source = BuildAggregatorSource(aggregatorName, descriptors);
        context.AddSource(AggregatorGeneratedFileName, source);
    }

    private static void GenerateRootExtensions(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<RegistrationAnalysis> analyses)
    {
        var localDescriptors = GetDescriptors(analyses);
        var referencedAggregators = FindReferencedAggregators(compilation);

        if (localDescriptors.Count == 0 && referencedAggregators.Count == 0)
        {
            return;
        }

        var source = BuildRootSource(localDescriptors, referencedAggregators);
        context.AddSource(GeneratedFileName, source);
    }

    private static List<RegistrationDescriptor> GetDescriptors(
        ImmutableArray<RegistrationAnalysis> analyses)
    {
        var descriptors = new List<RegistrationDescriptor>();

        foreach (var analysis in analyses)
        {
            if (analysis.HasDescriptors)
            {
                descriptors.AddRange(analysis.Descriptors);
            }
        }

        descriptors.Sort(CompareDescriptors);
        return descriptors;
    }

    private static List<AggregatorMethod> FindReferencedAggregators(Compilation compilation)
    {
        var aggregators = new List<AggregatorMethod>();
        var visitedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (visitedAssemblies.Add(assembly))
            {
                FindAggregatorsInNamespace(assembly.GlobalNamespace, aggregators);
            }
        }

        aggregators.Sort(static (left, right) =>
        {
            var contextComparison = StringComparer.Ordinal.Compare(
                left.ContextTypeName,
                right.ContextTypeName);

            return contextComparison != 0
                ? contextComparison
                : StringComparer.Ordinal.Compare(left.SortKey, right.SortKey);
        });

        return aggregators;
    }

    private static void FindAggregatorsInNamespace(
        INamespaceSymbol namespaceSymbol,
        List<AggregatorMethod> aggregators)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            if (HasAggregatorAttribute(type))
            {
                AddAggregatorMethods(type, aggregators);
            }

            FindAggregatorsInNestedTypes(type, aggregators);
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            FindAggregatorsInNamespace(childNamespace, aggregators);
        }
    }

    private static void FindAggregatorsInNestedTypes(
        INamedTypeSymbol type,
        List<AggregatorMethod> aggregators)
    {
        foreach (var nestedType in type.GetTypeMembers())
        {
            if (HasAggregatorAttribute(nestedType))
            {
                AddAggregatorMethods(nestedType, aggregators);
            }

            FindAggregatorsInNestedTypes(nestedType, aggregators);
        }
    }

    private static bool HasAggregatorAttribute(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat) == AggregatorAttributeMetadataName)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddAggregatorMethods(
        INamedTypeSymbol aggregator,
        List<AggregatorMethod> methods)
    {
        var aggregatorTypeName = aggregator.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        foreach (var method in aggregator.GetMembers(RegisterMethodName))
        {
            if (method is not IMethodSymbol
                {
                    IsStatic: true,
                    DeclaredAccessibility: Accessibility.Public,
                    Parameters.Length: 1 or 2
                } registrationMethod ||
                registrationMethod.Parameters[0].Type.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat) != ServiceCollectionMetadataName)
            {
                continue;
            }

            var contextTypeName = registrationMethod.Parameters.Length == 2
                ? registrationMethod.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : null;

            methods.Add(new AggregatorMethod(
                aggregatorTypeName,
                contextTypeName,
                aggregatorTypeName + "." + contextTypeName));
        }
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

    private static string BuildAggregatorSource(
        string aggregatorName,
        IReadOnlyList<RegistrationDescriptor> descriptors)
    {
        var builder = new StringBuilder(1024 + (descriptors.Count * 120));

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine();
        builder.Append("namespace ").Append(GeneratedNamespace).AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("[global::SharpServiceCollection.Attributes.ServiceRegistrationAggregator]");
        builder.Append("public static class ").Append(aggregatorName).AppendLine();
        builder.AppendLine("{");

        AppendAggregatorMethodGroups(builder, descriptors);

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string BuildRootSource(
        IReadOnlyList<RegistrationDescriptor> localDescriptors,
        IReadOnlyList<AggregatorMethod> aggregators)
    {
        var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var descriptor in localDescriptors)
        {
            var contextTypeName = descriptor.ContextTypeName ?? string.Empty;
            if (!groups.TryGetValue(contextTypeName, out var calls))
            {
                calls = new List<string>();
                groups.Add(contextTypeName, calls);
            }

            var call = descriptor.ContextTypeName is null
                ? $"        await new {descriptor.ImplementationTypeName}().RegisterAsync(services);"
                : $"        await new {descriptor.ImplementationTypeName}().RegisterAsync(services, context);";
            calls.Add(call);
        }

        foreach (var aggregator in aggregators)
        {
            var contextTypeName = aggregator.ContextTypeName ?? string.Empty;
            if (!groups.TryGetValue(contextTypeName, out var calls))
            {
                calls = new List<string>();
                groups.Add(contextTypeName, calls);
            }

            var call = aggregator.ContextTypeName is null
                ? $"        await {aggregator.AggregatorTypeName}.RegisterAsync(services);"
                : $"        await {aggregator.AggregatorTypeName}.RegisterAsync(services, context);";
            calls.Add(call);
        }

        var orderedGroups = new List<KeyValuePair<string, List<string>>>(groups);
        orderedGroups.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));

        var builder = new StringBuilder(1024);
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine();
        builder.Append("namespace ").Append(GeneratedNamespace).AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("public static class GeneratedServiceRegistrationExtensions");
        builder.AppendLine("{");

        foreach (var group in orderedGroups)
        {
            AppendRootMethod(builder, group.Key, group.Value);
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendAggregatorMethodGroups(
        StringBuilder builder,
        IReadOnlyList<RegistrationDescriptor> descriptors)
    {
        var groups = new Dictionary<string, List<RegistrationDescriptor>>(StringComparer.Ordinal);

        foreach (var descriptor in descriptors)
        {
            var contextTypeName = descriptor.ContextTypeName ?? string.Empty;
            if (!groups.TryGetValue(contextTypeName, out var registrations))
            {
                registrations = new List<RegistrationDescriptor>();
                groups.Add(contextTypeName, registrations);
            }

            registrations.Add(descriptor);
        }

        var orderedGroups = new List<KeyValuePair<string, List<RegistrationDescriptor>>>(groups);
        orderedGroups.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));

        foreach (var group in orderedGroups)
        {
            var contextTypeName = group.Key.Length == 0 ? null : group.Key;
            AppendAggregatorMethod(builder, contextTypeName, group.Value);
        }
    }

    private static void AppendAggregatorMethod(
        StringBuilder builder,
        string? contextTypeName,
        IReadOnlyList<RegistrationDescriptor> registrations)
    {
        builder.Append("    public static async Task<")
            .Append(ServiceCollectionType)
            .AppendLine("> RegisterAsync(");
        builder.Append("        ").Append(ServiceCollectionType).AppendLine(" services,");

        if (contextTypeName is not null)
        {
            builder.Append("        ").Append(contextTypeName).AppendLine(" context)");
        }
        else
        {
            builder.Length -= 2;
            builder.AppendLine(")");
        }

        builder.AppendLine("    {");
        foreach (var registration in registrations)
        {
            builder.Append("        await new ")
                .Append(registration.ImplementationTypeName)
                .AppendLine(contextTypeName is null
                    ? "().RegisterAsync(services);"
                    : "().RegisterAsync(services, context);");
        }

        builder.AppendLine("        return services;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendRootMethod(
        StringBuilder builder,
        string contextTypeName,
        IReadOnlyList<string> calls)
    {
        builder.Append("    public static async Task<")
            .Append(ServiceCollectionType)
            .AppendLine($"> {GeneratedMethodName}(");
        builder.Append("        this ").Append(ServiceCollectionType).AppendLine(" services,");

        if (contextTypeName.Length > 0)
        {
            builder.Append("        ").Append(contextTypeName).AppendLine(" context)");
        }
        else
        {
            builder.Length -= 2;
            builder.AppendLine(")");
        }

        builder.AppendLine("    {");
        foreach (var call in calls)
        {
            builder.AppendLine(call);
        }

        builder.AppendLine("        return services;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) || character == '_'
                ? character
                : '_');
        }

        if (builder.Length == 0 || char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }
}