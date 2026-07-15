using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Constants;
using SharpServiceCollection.Interfaces;
using SharpServiceCollection.InternalTypes;

namespace SharpServiceCollection.Generators;

[Generator]
public sealed class ServiceRegistrationGenerator : IIncrementalGenerator
{
    private const string LibraryNamespaceName = "SharpServiceCollection";
    private const string DisablePropertyName =
        "build_property.DisableServiceRegistrationGenerator";
    private const string AttributesNamespaceName = "Attributes";
    private const string InterfacesNamespaceName = "Interfaces";
    private const string GeneratedSubnamespaceName = "Generated";

    private const string InterfaceName = nameof(IServiceRegistration);
    private const string AttributeName = nameof(ServiceRegistrationItemAttribute);
    private const string AttributeMetadataName =
        $"{LibraryNamespaceName}.{AttributesNamespaceName}.{AttributeName}";

    private const string AggregatorAttributeName =
        nameof(ServiceRegistrationAggregatorAttribute);

    private const string GeneratedFileName = "SharpServiceCollection.ServiceRegistration.g.cs";
    private const string AggregatorGeneratedFileName = "SharpServiceCollection.ServiceRegistration.Aggregator.g.cs";
    private const string OrderPropertyName = "Order";
    private const string RootPropertyName = "ServiceRegistrationRoot";
    private const string RootDescSortOrderPropertyName = "ServiceRegistrationRootDescSortOrder";
    private const string GeneratedMethodName = "AddServiceRegistrationItemsAsync";
    private const string RegisterMethodName = "RegisterAsync";

    private const string ServiceCollectionType =
        "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";

    private const string ServiceCollectionMetadataName =
        "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";

    private const string GeneratedNamespace =
        $"{LibraryNamespaceName}.{GeneratedSubnamespaceName}";
    private const string AggregatorNamePrefix = "ServiceRegistrationAggregator_";

    private const string ServiceRegistrationMustBeSealedTitle = "Annotated class is not sealed";

    private const string ServiceRegistrationMustBeSealedDescription =
        "Types annotated with [ServiceRegistrationItem] must be sealed.";

    private const string ServiceRegistrationItemMustImplementInterfaceTitle =
        "Missing implementation of IServiceRegistration";

    private const string ServiceRegistrationMustImplementInterfaceDescription =
        "Types annotated with [ServiceRegistrationItem] must implement IServiceRegistration or IServiceRegistration<TContext>.";

    private static readonly DiagnosticDescriptor MustBeSealed = new(
        id: DiagnosticIds.ServiceRegistrationMustBeSealed,
        title: ServiceRegistrationMustBeSealedTitle,
        messageFormat: "Type '{0}' is decorated with [ServiceRegistrationItem] but is not sealed",
        category: SharedConsts.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ServiceRegistrationMustBeSealedDescription,
        helpLinkUri: string.Format(SharedConsts.HelpLinkUriFormat, "service-registration"));

    private static readonly DiagnosticDescriptor MustImplementInterface = new(
        id: DiagnosticIds.ServiceRegistrationMustImplementInterface,
        title: ServiceRegistrationItemMustImplementInterfaceTitle,
        messageFormat: "Type '{0}' must implement IServiceRegistration or IServiceRegistration<TContext>",
        category: SharedConsts.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ServiceRegistrationMustImplementInterfaceDescription,
        helpLinkUri: string.Format(SharedConsts.HelpLinkUriFormat, "service-registration"));



    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var isDisabled = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
                provider.GlobalOptions.TryGetValue(DisablePropertyName, out var value) &&
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

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
        // Only the assembly name is needed here, so avoid carrying the full
        // Compilation through this output pipeline.
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var projectSource = assemblyName
            .Combine(collectedCandidates)
            .Combine(isDisabled);
        context.RegisterSourceOutput(
            projectSource,
            static (spc, input) =>
            {
                if (input.Right)
                {
                    return;
                }

                GenerateProjectAggregator(
                    spc,
                    input.Left.Left,
                    input.Left.Right);
            });

        var isRoot = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
                provider.GlobalOptions.TryGetValue(
                    "build_property." + RootPropertyName,
                    out var value) &&
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        var isDescendingRootSortOrder = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                if (!provider.GlobalOptions.TryGetValue(
                        "build_property." + RootDescSortOrderPropertyName,
                        out var value))
                {
                    return true;
                }

                return !bool.TryParse(value, out var descending) || descending;
            });

        var rootSource = context.CompilationProvider
            .Combine(collectedCandidates)
            .Combine(isRoot)
            .Combine(isDescendingRootSortOrder)
            .Combine(isDisabled);

        context.RegisterSourceOutput(
            rootSource,
            static (spc, input) =>
            {
                if (input.Right || !input.Left.Left.Right)
                {
                    return;
                }

                GenerateRootExtensions(
                    spc,
                    input.Left.Left.Left.Left,
                    input.Left.Left.Left.Right,
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

            return new RegistrationAnalysis
            {
                Descriptors = [],
                Diagnostics = diagnostics.ToImmutable()
            };
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

            descriptors.Add(new RegistrationDescriptor
            {
                ImplementationTypeName = implementationTypeName,
                ContextType = contextType,
                ContextTypeName = contextTypeName,
                Order = order
            });
        }

        return new RegistrationAnalysis
        {
            Descriptors = descriptors.ToImmutable(),
            Diagnostics = []
        }; 
    }

    private static bool IsServiceRegistrationInterface(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol.Name != InterfaceName || interfaceSymbol.Arity is not (0 or 1))
        {
            return false;
        }

        var interfacesNamespace = interfaceSymbol.ContainingNamespace;
        var libraryNamespace = interfacesNamespace.ContainingNamespace;

        return interfacesNamespace.Name == InterfacesNamespaceName &&
               libraryNamespace.Name == LibraryNamespaceName &&
               libraryNamespace.ContainingNamespace.IsGlobalNamespace;
    }

    private static uint GetOrder(INamedTypeSymbol classSymbol)
    {
        foreach (var attribute in classSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat) !=
                $"global::{AttributeMetadataName}")
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
                        int value and >= 0 => (uint)value,
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
        ImmutableArray<RegistrationAnalysis> analyses,
        bool isDescendingRootSortOrder)
    {
        var localDescriptors = GetDescriptors(analyses);
        var referencedAggregators = FindReferencedAggregators(compilation);

        if (localDescriptors.Count == 0 && referencedAggregators.Count == 0)
        {
            return;
        }

        var source = BuildRootSource(
            compilation.AssemblyName,
            localDescriptors,
            referencedAggregators,
            isDescendingRootSortOrder);
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
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null || attributeClass.Name != AggregatorAttributeName)
            {
                continue;
            }

            var attributesNamespace = attributeClass.ContainingNamespace;
            var libraryNamespace = attributesNamespace.ContainingNamespace;

            if (attributesNamespace.Name == AttributesNamespaceName &&
                libraryNamespace.Name == LibraryNamespaceName &&
                libraryNamespace.ContainingNamespace.IsGlobalNamespace)
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

        foreach (var member in aggregator.GetMembers())
        {
            if (member is not IMethodSymbol
                {
                    IsStatic: true,
                    DeclaredAccessibility: Accessibility.Public,
                    Parameters.Length: 1 or 2
                } registrationMethod ||
                !TryGetOrder(registrationMethod.Name, out var order) ||
                registrationMethod.Parameters[0].Type.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat) != ServiceCollectionMetadataName)
            {
                continue;
            }

            var contextTypeName = registrationMethod.Parameters.Length == 2
                ? registrationMethod.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : null;

            methods.Add(new AggregatorMethod
            {
                AggregatorTypeName = aggregatorTypeName,
                MethodName = registrationMethod.Name,
                ContextTypeName = contextTypeName,
                Order = order,
                SortKey = $"{aggregatorTypeName}.{registrationMethod.Name}"
            });
        }
    }

    private static bool TryGetOrder(string methodName, out uint order)
    {
        const string prefix = RegisterMethodName + "_";
        if (!methodName.StartsWith(prefix, StringComparison.Ordinal))
        {
            order = 0;
            return false;
        }

        var orderStart = prefix.Length;
        var orderEnd = methodName.IndexOf('_', orderStart);
        var orderText = orderEnd < 0
            ? methodName.Substring(orderStart)
            : methodName.Substring(orderStart, orderEnd - orderStart);

        return uint.TryParse(orderText, out order);
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
        string? assemblyName,
        IReadOnlyList<RegistrationDescriptor> localDescriptors,
        IReadOnlyList<AggregatorMethod> aggregators,
        bool isDescendingRootSortOrder)
    {
        var groups = new Dictionary<string, List<RootRegistrationCall>>(StringComparer.Ordinal);

        // Call the host aggregator's RegisterAsync_{Order}_{index} methods (already
        // emitted by GenerateProjectAggregator) instead of inlining new Type().
        if (localDescriptors.Count > 0 && !string.IsNullOrWhiteSpace(assemblyName))
        {
            var aggregatorTypeName =
                $"global::{GeneratedNamespace}.{AggregatorNamePrefix}{SanitizeIdentifier(assemblyName!)}";

            for (var index = 0; index < localDescriptors.Count; index++)
            {
                var descriptor = localDescriptors[index];
                var methodName = RegisterMethodName + "_" + descriptor.Order + "_" + index;
                AddRootCall(
                    groups,
                    descriptor.ContextTypeName,
                    $"        await {aggregatorTypeName}.{methodName}(services" +
                    (descriptor.ContextTypeName is null ? ");" : ", context);"),
                    descriptor.Order,
                    $"{aggregatorTypeName}.{methodName}");
            }
        }

        foreach (var aggregator in aggregators)
        {
            AddRootCall(
                groups,
                aggregator.ContextTypeName,
                aggregator.ContextTypeName is null
                    ? $"        await {aggregator.AggregatorTypeName}.{aggregator.MethodName}(services);"
                    : $"        await {aggregator.AggregatorTypeName}.{aggregator.MethodName}(services, context);",
                aggregator.Order,
                aggregator.SortKey);
        }

        foreach (var calls in groups.Values)
        {
            calls.Sort((left, right) => CompareRootRegistrationCalls(
                left,
                right,
                isDescendingRootSortOrder));
        }

        var orderedGroups = new List<KeyValuePair<string, List<RootRegistrationCall>>>(groups);
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
        for (var index = 0; index < descriptors.Count; index++)
        {
            var descriptor = descriptors[index];
            var methodName = RegisterMethodName + "_" + descriptor.Order + "_" + index;
            AppendAggregatorMethod(builder, methodName, descriptor);
        }
    }

    private static void AppendAggregatorMethod(
        StringBuilder builder,
        string methodName,
        RegistrationDescriptor registration)
    {
        builder.Append("    public static async Task<")
            .Append(ServiceCollectionType)
            .Append("> ")
            .Append(methodName)
            .AppendLine("(");
        builder.Append("        ").Append(ServiceCollectionType).Append(" services");

        if (registration.ContextTypeName is not null)
        {
            builder.Append(",\n        ").Append(registration.ContextTypeName).AppendLine(" context)");
        }
        else
        {
            builder.AppendLine(")");
        }

        builder.AppendLine("    {");
        builder.Append("        await new ")
            .Append(registration.ImplementationTypeName)
            .AppendLine(registration.ContextTypeName is null
                ? "().RegisterAsync(services);"
                : "().RegisterAsync(services, context);");

        builder.AppendLine("        return services;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AddRootCall(
        Dictionary<string, List<RootRegistrationCall>> groups,
        string? contextTypeName,
        string call,
        uint order,
        string sortKey)
    {
        var key = contextTypeName ?? string.Empty;
        if (!groups.TryGetValue(key, out var calls))
        {
            calls = [];
            groups.Add(key, calls);
        }

        calls.Add(new RootRegistrationCall
        {
            Call = call,
            Order = order,
            SortKey = sortKey
        });
    }

    private static int CompareRootRegistrationCalls(
        RootRegistrationCall left,
        RootRegistrationCall right,
        bool isDescendingRootSortOrder)
    {
        var orderComparison = isDescendingRootSortOrder
            ? right.Order.CompareTo(left.Order)
            : left.Order.CompareTo(right.Order);

        return orderComparison != 0
            ? orderComparison
            : StringComparer.Ordinal.Compare(left.SortKey, right.SortKey);
    }

    private static void AppendRootMethod(
        StringBuilder builder,
        string contextTypeName,
        IReadOnlyList<RootRegistrationCall> calls)
    {
        builder.Append("    internal static async Task<")
            .Append(ServiceCollectionType)
            .AppendLine($"> {GeneratedMethodName}(");
        builder.Append("        this ").Append(ServiceCollectionType).Append(" services");

        if (contextTypeName.Length > 0)
        {
            builder.Append(",\n        ").Append(contextTypeName).AppendLine(" context)");
        }
        else
        {
            builder.AppendLine(")");
        }

        builder.AppendLine("    {");
        foreach (var call in calls)
        {
            builder.AppendLine(call.Call);
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