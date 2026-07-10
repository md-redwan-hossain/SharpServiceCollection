using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SharpServiceCollection.SourceGenerator.InternalTypes;
using static SharpServiceCollection.SourceGenerator.InternalTypes.GeneratorConstants;
using static SharpServiceCollection.SourceGenerator.InternalTypes.GeneratorConstants.DependencyInjection;
using static SharpServiceCollection.SourceGenerator.InternalTypes.GeneratorConstants.ServiceRegistration;

namespace SharpServiceCollection.SourceGenerator.Generators;

[Generator]
public sealed class ServiceRegistrationGenerator : IIncrementalGenerator
{
    private const string Indent = "        ";

    // Always LF — valid C# on every OS, and avoids RS1035 (Environment is banned in analyzers).
    // AppendLine would embed the build machine's newline and break cross-platform diffs.
    private const string NewLine = "\n";

    private readonly record struct TypeAnalysisResult(ImmutableArray<Diagnostic> Diagnostics);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeResults = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 },
                transform: static (ctx, _) => AnalyzeClass(ctx))
            .Where(static result => result.HasValue)
            .Select(static (result, _) => result.GetValueOrDefault());

        var collectedResults = typeResults.Collect();

        var compilationAndOptions = context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider);

        var combined = collectedResults.Combine(compilationAndOptions);

        context.RegisterSourceOutput(combined,
            static (spc, source) => EmitGeneratedCode(spc, source.Left, source.Right.Left, source.Right.Right));
    }

    private static TypeAnalysisResult? AnalyzeClass(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        if (ctx.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        if (!IsServiceRegistrationBase(symbol.BaseType))
        {
            return null;
        }

        var diagnostics = new List<Diagnostic>();
        CollectValidationDiagnostics(symbol, classDeclaration, diagnostics);

        if (diagnostics.Count == 0)
        {
            return null;
        }

        return new TypeAnalysisResult([..diagnostics]);
    }

    private static void EmitGeneratedCode(
        SourceProductionContext context,
        ImmutableArray<TypeAnalysisResult> results,
        Compilation compilation,
        AnalyzerConfigOptionsProvider optionsProvider)
    {
        foreach (var result in results)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        if (string.Equals(compilation.AssemblyName, RuntimeAssemblyName, StringComparison.Ordinal))
        {
            return;
        }

        if (!IsServiceRegistrationRoot(optionsProvider))
        {
            return;
        }

        var items = CollectItemsFromReferencedAssemblies(compilation);
        var generatedSource = BuildSource(items);
        context.AddSource(ServiceRegistration.GeneratedFileName, SourceText.From(generatedSource, Encoding.UTF8));
    }

    private static bool IsServiceRegistrationRoot(AnalyzerConfigOptionsProvider optionsProvider)
    {
        if (!optionsProvider.GlobalOptions.TryGetValue(MsBuildPropertyKey, out var value))
        {
            return false;
        }

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
    }

    private static void CollectValidationDiagnostics(
        INamedTypeSymbol typeSymbol,
        ClassDeclarationSyntax classDeclaration,
        ICollection<Diagnostic> diagnostics)
    {
        var location = classDeclaration.Identifier.GetLocation();

        if (typeSymbol.Name != ClassName)
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.ServiceRegistrationMustBeNamed,
                location,
                typeSymbol.ToDisplayString()));
        }

        if (!typeSymbol.IsSealed)
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.ServiceRegistrationMustBeSealed,
                location,
                typeSymbol.ToDisplayString()));
        }

        if (typeSymbol is { Name: ClassName, IsSealed: true }
            && !HasAccessibleParameterlessConstructor(typeSymbol))
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.ServiceRegistrationMissingParameterlessConstructor,
                location,
                typeSymbol.ToDisplayString()));
        }
    }

    private static bool IsServiceRegistrationBase(INamedTypeSymbol? baseType)
    {
        return IsNonGenericServiceRegistrationBase(baseType) || IsGenericServiceRegistrationBase(baseType);
    }

    private static bool IsNonGenericServiceRegistrationBase(INamedTypeSymbol? typeSymbol)
    {
        return typeSymbol is { IsGenericType: false, Name: BaseTypeName }
               && typeSymbol.ContainingNamespace.ToDisplayString() == BaseTypeNamespace;
    }

    private static bool IsGenericServiceRegistrationBase(INamedTypeSymbol? typeSymbol)
    {
        return typeSymbol is { IsGenericType: true, ConstructedFrom.Name: BaseTypeName }
               && typeSymbol.ConstructedFrom.ContainingNamespace.ToDisplayString() == BaseTypeNamespace;
    }

    // Used only for the in-compilation diagnostic check: is this class's own shape sane?
    // (Not for deciding whether the generated aggregator can call the constructor —
    // see HasConstructorAccessibleFrom for that.)
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

    // Used when the item lives in a referenced assembly and the generated code in the
    // root assembly needs to call `new {Item}()` directly. Accounts for InternalsVisibleTo,
    // unlike the simple Public-or-Internal heuristic above.
    private static bool HasConstructorAccessibleFrom(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        foreach (var constructor in typeSymbol.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 0
                && compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<ItemRegistrationDescriptor> CollectItemsFromReferencedAssemblies(
        Compilation compilation)
    {
        var nonGenericBase = compilation.GetTypeByMetadataName(BaseTypeMetadataName);
        var genericBase = compilation.GetTypeByMetadataName(BaseTypeMetadataName + "`1");
        if (nonGenericBase is null && genericBase is null)
        {
            return [];
        }

        var items = new List<ItemRegistrationDescriptor>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in EnumerateTypesInReferencedAssemblies(compilation))
        {
            var descriptor = TryCreateDescriptor(type, nonGenericBase, genericBase, compilation);
            if (descriptor is null)
            {
                continue;
            }

            if (seen.Add(descriptor.FullyQualifiedTypeName))
            {
                items.Add(descriptor);
            }
        }

        return items
            .OrderBy(i => i.FullyQualifiedTypeName, StringComparer.Ordinal)
            .ToList();
    }

    private static ItemRegistrationDescriptor? TryCreateDescriptor(
        INamedTypeSymbol type,
        INamedTypeSymbol? nonGenericBase,
        INamedTypeSymbol? genericBase,
        Compilation compilation)
    {
        if (type.Name != ClassName || !type.IsSealed || type.IsAbstract)
        {
            return null;
        }

        if (!HasConstructorAccessibleFrom(type, compilation))
        {
            return null;
        }

        if (nonGenericBase is not null && SymbolEqualityComparer.Default.Equals(type.BaseType, nonGenericBase))
        {
            return new ItemRegistrationDescriptor
            {
                FullyQualifiedTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ContextTypeName = null,
                IsGeneric = false
            };
        }

        if (genericBase is not null
            && type.BaseType is { IsGenericType: true } constructed
            && SymbolEqualityComparer.Default.Equals(constructed.ConstructedFrom, genericBase)
            && constructed.TypeArguments.Length == 1)
        {
            return new ItemRegistrationDescriptor
            {
                FullyQualifiedTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ContextTypeName = constructed.TypeArguments[0]
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsGeneric = true
            };
        }

        return null;
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypesInReferencedAssemblies(Compilation compilation)
    {
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
            {
                continue;
            }

            if (IsSystemAssembly(assembly))
            {
                continue;
            }

            foreach (var type in EnumerateTypes(assembly.GlobalNamespace))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol namespaceSymbol)
    {
        // Fast path: top-level types named exactly ClassName, filtered by the symbol table
        // itself rather than materializing every type and filtering afterward.
        foreach (var type in namespaceSymbol.GetTypeMembers(ClassName))
        {
            yield return type;
        }

        // Still need to walk all types (regardless of name) to find nested candidates,
        // since GetTypeMembers(name) only filters the current level.
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            foreach (var nested in EnumerateNestedTypes(type))
            {
                yield return nested;
            }
        }

        foreach (var child in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in EnumerateTypes(child))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers(ClassName))
        {
            yield return nested;
        }

        foreach (var nested in type.GetTypeMembers())
        {
            foreach (var deeper in EnumerateNestedTypes(nested))
            {
                yield return deeper;
            }
        }
    }

    private static bool IsSystemAssembly(IAssemblySymbol assembly)
    {
        var name = assembly.Name;
        return name.StartsWith("System", StringComparison.Ordinal)
               || name.StartsWith("Microsoft", StringComparison.Ordinal)
               || name.StartsWith("netstandard", StringComparison.Ordinal)
               || name.StartsWith("mscorlib", StringComparison.Ordinal)
               || string.Equals(name, RuntimeAssemblyName, StringComparison.Ordinal);
    }

    private static string BuildSource(IReadOnlyCollection<ItemRegistrationDescriptor> items)
    {
        var nonGeneric = new List<ItemRegistrationDescriptor>();
        var genericByContext = new SortedDictionary<string, List<ItemRegistrationDescriptor>>(StringComparer.Ordinal);

        foreach (var item in items)
        {
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
                group = [];
                genericByContext[contextTypeName] = group;
            }

            group.Add(item);
        }

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
        if (items.Count == 0)
        {
            AppendSourceLine(
                builder,
                $$"""
                      public static async Task<{{ServiceCollectionType}}> {{HostMethodName}}(
                          this {{ServiceCollectionType}} services)
                      {
                  {{Indent}}await Task.CompletedTask;
                  {{Indent}}return services;
                      }
                  """);
            AppendSourceLine(builder);
            return;
        }

        AppendSourceLine(
            builder,
            $$"""
                  public static async Task<{{ServiceCollectionType}}> {{HostMethodName}}(
                      this {{ServiceCollectionType}} services)
                  {
              """);

        AppendItemsLoopBody(
            builder,
            items,
            arrayTypeDeclaration: $"global::{BaseTypeMetadataName}[]",
            executeArguments: "services");

        AppendSourceLine(builder, "    }");
        AppendSourceLine(builder);
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

        AppendItemsLoopBody(
            builder,
            items,
            arrayTypeDeclaration: $"global::{BaseTypeMetadataName}<{contextTypeName}>[]",
            executeArguments: "services, context");

        AppendSourceLine(builder, "    }");
        AppendSourceLine(builder);
    }

    private static void AppendItemsLoopBody(
        StringBuilder builder,
        IReadOnlyList<ItemRegistrationDescriptor> items,
        string arrayTypeDeclaration,
        string executeArguments)
    {
        AppendSourceLine(builder, $"{Indent}{arrayTypeDeclaration} items =");
        AppendSourceLine(builder, $"{Indent}[");
        foreach (var item in items)
        {
            AppendSourceLine(builder, $"{Indent}    new {item.FullyQualifiedTypeName}(),");
        }

        AppendSourceLine(builder, $"{Indent}];");
        AppendSourceLine(builder);
        AppendSourceLine(builder, $"{Indent}foreach (var item in items.OrderByDescending(i => i.Priority))");
        AppendSourceLine(builder, $"{Indent}{{");
        AppendSourceLine(builder, $"{Indent}    await item.{ExecuteMethodName}({executeArguments});");
        AppendSourceLine(builder, $"{Indent}}}");
        AppendSourceLine(builder);
        AppendSourceLine(builder, $"{Indent}return services;");
    }

    private static void AppendSourceLine(StringBuilder builder, string? line = null)
    {
        if (line is not null)
        {
            builder.Append(line);
        }

        builder.Append(NewLine);
    }
}