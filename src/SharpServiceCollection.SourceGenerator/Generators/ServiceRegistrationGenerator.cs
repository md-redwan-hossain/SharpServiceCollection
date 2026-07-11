using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SharpServiceCollection.SourceGenerator.InternalTypes;
using static SharpServiceCollection.SourceGenerator.InternalTypes.GeneratorConstants;
using static SharpServiceCollection.SourceGenerator.InternalTypes.GeneratorConstants.AttributeMetadata;
using static SharpServiceCollection.SourceGenerator.InternalTypes.GeneratorConstants.DependencyInjection;
using static SharpServiceCollection.SourceGenerator.InternalTypes.GeneratorConstants.ServiceRegistration;
using static SharpServiceCollection.SourceGenerator.InternalTypes.GeneratorConstants.TrackingNames;


namespace SharpServiceCollection.SourceGenerator.Generators;

[Generator]
public sealed class ServiceRegistrationGenerator : IIncrementalGenerator
{
    private const string Indent = "        ";

    private readonly record struct ItemAndDiagnostics(
        ItemRegistrationDescriptor? Item,
        ImmutableArray<Diagnostic> Diagnostics)
    {
        public static readonly ItemAndDiagnostics Empty = default;
        public bool IsEmpty => Item is null;
    }

    private readonly record struct CompilationContext(Compilation Compilation, bool IsServiceRegistrationRoot);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // (1) Non-generic attribute stream: [ServiceRegistration].
        var nonGenericItems = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ServiceRegistrationMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => AnalyzeAttribute(ctx, isGeneric: false))
            .Where(static result => !result.IsEmpty)
            .WithTrackingName(ServiceRegistrationNonGeneric);

        // (2) Generic attribute stream: [ServiceRegistration<T>].
        var genericItems = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ServiceRegistrationGenericMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => AnalyzeAttribute(ctx, isGeneric: true))
            .Where(static result => !result.IsEmpty)
            .WithTrackingName(ServiceRegistrationGeneric);

        // (3) Combine the two attribute streams into a single collected array.
        // Multi-to-multi Combine is forbidden, so each stream is collected first.
        var collected = nonGenericItems
            .Collect()
            .Combine(genericItems.Collect())
            .Select(static (tuple, _) => MergeStreams(tuple.Left, tuple.Right))
            .WithTrackingName(ServiceRegistrationCollectDiagnostics);

        // (4) Combine the merged items with the compilation context. The
        // root-marker bool is read from the MSBuild `ServiceRegistrationRoot`
        // property (exposed via `CompilerVisibleProperty` in
        // `Directory.Build.props` / `buildTransitive/SharpServiceCollection.props`)
        // and resolved through `AnalyzerConfigOptionsProvider`.
        var compilationContext = context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (tuple, _) => new CompilationContext(
                Compilation: tuple.Left,
                IsServiceRegistrationRoot: ReadServiceRegistrationRootFlag(tuple.Right)))
            .WithTrackingName(ServiceRegistrationCombineCompilation);

        var combined = collected.Combine(compilationContext);

        context.RegisterSourceOutput(combined,
            static (spc, source) => EmitGeneratedCode(
                spc,
                source.Left,
                source.Right.Compilation,
                source.Right.IsServiceRegistrationRoot));
    }

    private static ItemAndDiagnostics AnalyzeAttribute(
        GeneratorAttributeSyntaxContext ctx,
        bool isGeneric)
    {
        try
        {
            return AnalyzeAttributeCore(ctx, isGeneric);
        }
        catch (Exception ex)
        {
            string detail = string.Concat(
                ex.GetType().Name, ": ", ex.Message,
                " | targetNodeType=", ctx.TargetNode.GetType().Name,
                " | targetSymbolKind=", ctx.TargetSymbol.Kind.ToString(),
                " | isGeneric=", isGeneric.ToString());

            return new ItemAndDiagnostics(null,
                ImmutableArray.Create(Diagnostic.Create(
                    GeneratorDiagnostics.DebugDiagnostic,
                    ctx.TargetNode.GetLocation(),
                    detail)));
        }
    }

    private static ItemAndDiagnostics AnalyzeAttributeCore(
        GeneratorAttributeSyntaxContext ctx,
        bool isGeneric)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol symbol)
        {
            return ItemAndDiagnostics.Empty;
        }

        // Resolve the [ServiceRegistration] / [ServiceRegistration<T>] attribute
        // that matched the stream. There should be exactly one (AllowMultiple=false).
        AttributeData? attribute = null;
        foreach (var candidate in symbol.GetAttributes())
        {
            if (candidate.AttributeClass is { } attrClass
                && attrClass.ToDisplayString() == (isGeneric
                    ? ServiceRegistrationGenericMetadataName
                    : ServiceRegistrationMetadataName))
            {
                attribute = candidate;
                break;
            }
        }

        if (attribute is null)
        {
            return ItemAndDiagnostics.Empty;
        }

        if (!TryReadOrder(attribute, out var order))
        {
            // Syntax was caught by the predicate but the attribute isn't actually
            // ours in the semantic model (shouldn't happen, but defend anyway).
            return ItemAndDiagnostics.Empty;
        }

        // The pipeline predicate restricts us to ClassDeclarationSyntax nodes, so
        // ctx.TargetNode is already a ClassDeclarationSyntax - no cast fallback needed.
        var classDeclaration = (ClassDeclarationSyntax)ctx.TargetNode;

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        if (!symbol.IsSealed)
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.ServiceRegistrationMustBeSealed,
                classDeclaration.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        if (!HasAccessibleParameterlessConstructor(symbol))
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.ServiceRegistrationMissingParameterlessConstructor,
                classDeclaration.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        if (!HasExecuteAsync(symbol, isGeneric))
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.ServiceRegistrationMustImplementExecuteAsync,
                classDeclaration.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        // Only emit the descriptor if the surface checks pass (so we don't ship
        // misconfigured entries into the generated array).
        ItemRegistrationDescriptor? item = diagnostics.Count == 0
            ? new ItemRegistrationDescriptor
            {
                FullyQualifiedTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ContextTypeName = isGeneric
                    ? symbol.TypeArguments.Length == 1
                        ? symbol.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        : null
                    : null,
                IsGeneric = isGeneric,
                Order = order
            }
            : null;

        return new ItemAndDiagnostics(item, diagnostics.ToImmutable());
    }

    private static bool TryReadOrder(AttributeData attribute, out uint order)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == AttributeProperties.Order)
            {
                if (argument.Value.Value is uint u)
                {
                    order = u;
                    return true;
                }

                if (argument.Value.Value is int i and >= 0)
                {
                    order = (uint)i;
                    return true;
                }
            }
        }

        // Default for [ServiceRegistration] (Order is a uint property, defaults to 0).
        order = 0;
        return true;
    }

    private static ImmutableArray<ItemAndDiagnostics> MergeStreams(
        ImmutableArray<ItemAndDiagnostics> left,
        ImmutableArray<ItemAndDiagnostics> right)
    {
        var builder = ImmutableArray.CreateBuilder<ItemAndDiagnostics>(left.Length + right.Length);
        builder.AddRange(left);
        builder.AddRange(right);
        return builder.ToImmutable();
    }

    private static void EmitGeneratedCode(
        SourceProductionContext context,
        ImmutableArray<ItemAndDiagnostics> items,
        Compilation compilation,
        bool isServiceRegistrationRoot)
    {
        var nonGeneric = new List<ItemRegistrationDescriptor>();
        var genericByContext = new SortedDictionary<string, List<ItemRegistrationDescriptor>>(StringComparer.Ordinal);

        foreach (var entry in items)
        {
            foreach (var diagnostic in entry.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            var item = entry.Item;
            if (item is null)
            {
                continue;
            }

            if (string.Equals(compilation.AssemblyName, RuntimeAssemblyName, StringComparison.Ordinal))
            {
                // Don't emit code that references itself.
                continue;
            }

            if (!item.IsGeneric)
            {
                nonGeneric.Add(item);
                continue;
            }

            var contextTypeName = item.ContextTypeName;
            if (contextTypeName is null)
            {
                continue;
            }

            if (!genericByContext.TryGetValue(contextTypeName, out var group))
            {
                group = new List<ItemRegistrationDescriptor>();
                genericByContext[contextTypeName] = group;
            }

            group.Add(item);
        }

        if (!isServiceRegistrationRoot)
        {
            return;
        }

        if (nonGeneric.Count == 0 && genericByContext.Count == 0)
        {
            return;
        }

        // Pre-sort by Order ascending, ties broken by fully-qualified type name ascending.
        // The emitted loop still runs `OrderByDescending(i => i.Order)` so each contextual
        // generator runs later, matching the user's intuition that "higher Order runs later".
        nonGeneric.Sort(static (a, b) =>
        {
            var cmp = a.Order.CompareTo(b.Order);
            return cmp != 0 ? cmp : StringComparer.Ordinal.Compare(a.FullyQualifiedTypeName, b.FullyQualifiedTypeName);
        });

        foreach (var pair in genericByContext)
        {
            pair.Value.Sort(static (a, b) =>
            {
                var cmp = a.Order.CompareTo(b.Order);
                return cmp != 0
                    ? cmp
                    : StringComparer.Ordinal.Compare(a.FullyQualifiedTypeName, b.FullyQualifiedTypeName);
            });
        }

        var generatedSource = BuildSource(nonGeneric, genericByContext);
        context.AddSource(ServiceRegistration.GeneratedFileName, SourceText.From(generatedSource, Encoding.UTF8));
    }

    private static bool ReadServiceRegistrationRootFlag(AnalyzerConfigOptionsProvider optionsProvider)
    {
        // The MSBuild property `<ServiceRegistrationRoot>true</ServiceRegistrationRoot>`
        // is surfaced to Roslyn as `build_property.ServiceRegistrationRoot` via the
        // `CompilerVisibleProperty` element in `Directory.Build.props` (in-repo) and
        // `buildTransitive/SharpServiceCollection.props` (NuGet consumer). Only the
        // root project emits extensions; every other compilation is a passthrough.
        return optionsProvider.GlobalOptions.TryGetValue(
                   "build_property.ServiceRegistrationRoot",
                   out var value)
               && !string.IsNullOrEmpty(value)
               && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(value, "0", StringComparison.Ordinal);
    }

    private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol typeSymbol)
    {
        foreach (var constructor in typeSymbol.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 0
                && constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            {
                return true;
            }
        }

        return false;
    }

    // SSR008 must implement ExecuteAsync. The non-generic variant takes one parameter
    // (the IServiceCollection); the generic variant takes two (the IServiceCollection
    // plus the context type). We don't bind to a base class anymore — the [ServiceRegistration]
    // attribute is the only contract, so the user is free to name/structure the class.
    private static bool HasExecuteAsync(INamedTypeSymbol typeSymbol, bool isGeneric)
    {
        var expectedParameters = isGeneric ? 2 : 1;

        foreach (var member in typeSymbol.GetMembers(ExecuteMethodName))
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            if (method.IsStatic)
            {
                continue;
            }

            if (method.Parameters.Length != expectedParameters)
            {
                continue;
            }

            if (method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildSource(
        IReadOnlyList<ItemRegistrationDescriptor> nonGeneric,
        IReadOnlyDictionary<string, List<ItemRegistrationDescriptor>> genericByContext)
    {
        var methodsSource = BuildMethodsSource(nonGeneric, genericByContext);

        return $$"""
                 // <auto-generated />
                 #nullable enable
                 using System.Linq;
                 using System.Threading.Tasks;
                 using Microsoft.Extensions.DependencyInjection;

                 namespace {{GeneratedCode.Namespace}};

                 public static class {{ExtensionsClassName}}
                 {
                 {{methodsSource}}
                 }

                 """;
    }

    private static string BuildMethodsSource(
        IReadOnlyList<ItemRegistrationDescriptor> nonGeneric,
        IReadOnlyDictionary<string, List<ItemRegistrationDescriptor>> genericByContext)
    {
        var builder = new StringBuilder();
        AppendNonGenericMethod(builder, nonGeneric);

        foreach (var pair in genericByContext)
        {
            AppendGenericMethod(builder, pair.Key, pair.Value);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendNonGenericMethod(
        StringBuilder builder,
        IReadOnlyList<ItemRegistrationDescriptor> items)
    {
        AppendSourceLine(
            builder,
            $$"""
                  public static async Task<{{ServiceCollectionType}}> {{HostMethodName}}(
                      this {{ServiceCollectionType}} services)
                  {
              """);

        AppendNonGenericItemCalls(builder, items);
        AppendSourceLine(builder, $"{Indent}return services;");
        AppendSourceLine(builder, "    }");
        AppendSourceLine(builder);
    }

    private static void AppendNonGenericItemCalls(
        StringBuilder builder,
        IReadOnlyList<ItemRegistrationDescriptor> items)
    {
        // Each registration is emitted as its own `await new T().ExecuteAsync(...)`
        // call, so we sidestep the "what's the array element type?" question.
        // This is a generation-time sort: the items are already ordered by
        // `Order` ascending (ties broken by FQN), so no `.OrderByDescending`
        // runs at runtime, satisfying the post-migration requirement.
        foreach (var item in items)
        {
            AppendSourceLine(builder,
                $"{Indent}await new {item.FullyQualifiedTypeName}().{ExecuteMethodName}(services);");
        }
    }

    private static void AppendGenericMethod(
        StringBuilder builder,
        string contextTypeName,
        IReadOnlyList<ItemRegistrationDescriptor> items)
    {
        AppendSourceLine(
            builder,
            $$"""
                  public static async Task<{{ServiceCollectionType}}> {{HostMethodName}}(
                      this {{ServiceCollectionType}} services,
                      {{contextTypeName}} context)
                  {
              """);

        AppendGenericItemCalls(builder, items);
        AppendSourceLine(builder, $"{Indent}return services;");
        AppendSourceLine(builder, "    }");
        AppendSourceLine(builder);
    }

    private static void AppendGenericItemCalls(
        StringBuilder builder,
        IReadOnlyList<ItemRegistrationDescriptor> items)
    {
        foreach (var item in items)
        {
            AppendSourceLine(builder,
                $"{Indent}await new {item.FullyQualifiedTypeName}().{ExecuteMethodName}(services, context);");
        }
    }

    private static void AppendSourceLine(StringBuilder builder, string? line = null)
    {
        if (line is not null)
        {
            builder.Append(line);
        }

        builder.AppendLine();
    }
}