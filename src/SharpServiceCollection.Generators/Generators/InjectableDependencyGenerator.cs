using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SharpServiceCollection.Generators.InternalTypes;
using static SharpServiceCollection.Generators.InternalTypes.GeneratorConstants;
using static SharpServiceCollection.Generators.InternalTypes.GeneratorConstants.DependencyInjection;
using static SharpServiceCollection.Generators.InternalTypes.GeneratorConstants.TrackingNames;

namespace SharpServiceCollection.Generators.Generators;

// Optimised following Andrew Lock's "Creating a Source Generator - Part 9":
// https://andrewlock.net/creating-a-source-generator-part-9-avoiding-performance-pitfalls-in-incremental-generators/
//
// Key wins:
//   1. `ForAttributeWithMetadataName` pre-filters to nodes that actually carry
//      `InjectableDependencyAttribute` (non-generic or unbound arity-1). The
//      predicate stays purely syntactic - no semantic work, no allocations.
//   2. The per-class transform returns a struct so we avoid allocating a fresh
//      `List<>` per invalidation. Empty results are dropped before downstream
//      stages.
//   3. We only `Combine` with the assembly name - never with the full
//      `Compilation` - so an edit that doesn't touch the assembly name doesn't
//      re-run the emission stage.
//   4. Every pipeline stage is decorated with `WithTrackingName` so the
//      generator's behaviour is visible in incremental-gen traces.
[Generator]
public sealed class InjectableDependencyGenerator : IIncrementalGenerator
{
    private const string Indent = "        ";
    private const string TypeOfPrefix = "typeof(";
    private const string TypeOfSuffix = ")";

    private const string NonGenericAttributeMetadataName =
        "SharpServiceCollection.Attributes.InjectableDependencyAttribute";
    private const string GenericAttributeMetadataName =
        "SharpServiceCollection.Attributes.InjectableDependencyAttribute`1";
    
    private readonly record struct TypeRegistrationResult(
        ImmutableArray<ServiceRegistrationDescriptor> Descriptors,
        ImmutableArray<Diagnostic> Diagnostics)
    {
        public static readonly TypeRegistrationResult Empty = new([], []);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // (1) Two pre-filtered streams: the non-generic attribute has metadata
        // name `InjectableDependencyAttribute`, while the unbound generic
        // `InjectableDependencyAttribute<T>` is reported as `...Attribute`1`.
        // ForAttributeWithMetadataName requires both shapes to be queried
        // explicitly - the predicate stays purely syntactic.
        var nonGenericStream = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                NonGenericAttributeMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => AnalyzeClass(ctx))
            .WithTrackingName(InjectableDependencyNonGeneric);

        var genericStream = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenericAttributeMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => AnalyzeClass(ctx))
            .WithTrackingName(InjectableDependencyGeneric);

        // (2) Collect each stream independently and combine. Cross-stream
        // duplicates are absorbed by the deduplication in `EmitGeneratedCode`.
        // We deliberately avoid combining with the full `Compilation` - the
        // assembly name is sufficient and cheap to invalidate on.
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var combined = nonGenericStream
            .Collect()
            .WithTrackingName(InjectableDependencyCollectNonGeneric)
            .Combine(genericStream.Collect().WithTrackingName(InjectableDependencyCollectGeneric))
            .WithTrackingName(InjectableDependencyCombineStreams)
            .Combine(assemblyName)
            .WithTrackingName(InjectableDependencyCombineAssembly);

        context.RegisterSourceOutput(combined,
            static (spc, source) => EmitGeneratedCode(
                spc,
                source.Left.Left,
                source.Left.Right,
                source.Right));
    }

    private static TypeRegistrationResult AnalyzeClass(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol symbol)
        {
            return TypeRegistrationResult.Empty;
        }

        var registrations = new List<RegistrationModel>();
        var diagnostics = new List<Diagnostic>();
        CollectRegistrations(symbol, registrations, diagnostics);

        var descriptors = ExpandRegistrations(registrations, diagnostics);

        if (descriptors.Count == 0 && diagnostics.Count == 0)
        {
            return TypeRegistrationResult.Empty;
        }

        return new TypeRegistrationResult([..descriptors], [..diagnostics]);
    }

    private static void EmitGeneratedCode(
        SourceProductionContext context,
        ImmutableArray<TypeRegistrationResult> nonGenericResults,
        ImmutableArray<TypeRegistrationResult> genericResults,
        string? assemblyName)
    {
        if (string.Equals(assemblyName, RuntimeAssemblyName, StringComparison.Ordinal))
        {
            return;
        }

        var descriptors = new List<ServiceRegistrationDescriptor>();
        var seen = new HashSet<(string ServiceTypeName, string ImplementationTypeName, string Key,
            RegistrationLifetime Lifetime, bool TryAdd, bool Enumerable)>();

        CollectDescriptors(nonGenericResults, descriptors, seen, context);
        CollectDescriptors(genericResults, descriptors, seen, context);

        var sorted = descriptors
            .OrderBy(r => r.Order)
            .ThenBy(r => r.ImplementationNameSortKey, StringComparer.Ordinal)
            .ToList();

        var generatedSource = BuildSource(sorted, assemblyName);
        context.AddSource(GeneratedFileName, SourceText.From(generatedSource, Encoding.UTF8));
    }

    private static void CollectDescriptors(
        ImmutableArray<TypeRegistrationResult> results,
        List<ServiceRegistrationDescriptor> descriptors,
        HashSet<(string ServiceTypeName, string ImplementationTypeName, string Key,
            RegistrationLifetime Lifetime, bool TryAdd, bool Enumerable)> seen,
        SourceProductionContext context)
    {
        foreach (var result in results)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            foreach (var descriptor in result.Descriptors)
            {
                var key = (descriptor.ServiceTypeName, descriptor.ImplementationTypeName, descriptor.Key,
                    descriptor.Lifetime, descriptor.TryAdd, descriptor.Enumerable);

                if (seen.Add(key))
                {
                    descriptors.Add(descriptor);
                }
            }
        }
    }

    private static void CollectRegistrations(
        INamedTypeSymbol typeSymbol,
        ICollection<RegistrationModel> registrations,
        ICollection<Diagnostic> diagnostics)
    {
        foreach (var attribute in typeSymbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null)
            {
                continue;
            }

            if (IsNonGenericInjectableDependencyAttribute(attributeClass))
            {
                var registration = CreateNonGenericRegistration(typeSymbol, attribute, diagnostics);
                if (registration is not null)
                {
                    registrations.Add(registration);
                }
            }
            else if (IsGenericInjectableDependencyAttribute(attributeClass))
            {
                var registration = CreateGenericRegistration(typeSymbol, attribute, diagnostics);
                if (registration is not null)
                {
                    registrations.Add(registration);
                }
            }
        }
    }

    private static bool IsNonGenericInjectableDependencyAttribute(INamedTypeSymbol attributeClass)
    {
        return attributeClass is { IsGenericType: false, Name: AttributeMetadata.Name }
               && attributeClass.ContainingNamespace.ToDisplayString() == AttributeMetadata.Namespace;
    }

    private static bool IsGenericInjectableDependencyAttribute(INamedTypeSymbol attributeClass)
    {
        return attributeClass is { IsGenericType: true, ConstructedFrom.Name: AttributeMetadata.Name }
               && attributeClass.ConstructedFrom.ContainingNamespace.ToDisplayString() == AttributeMetadata.Namespace;
    }

    private static RegistrationModel? CreateNonGenericRegistration(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        ICollection<Diagnostic> diagnostics)
    {
        if (attribute.ConstructorArguments.Length != 2)
        {
            return null;
        }

        if (!TryParseLifetime(attribute.ConstructorArguments[0], out var lifetime))
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.InvalidLifetime,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                attribute.ConstructorArguments[0].Value?.ToString() ?? "null",
                implementationType.ToDisplayString()));
            return null;
        }

        if (!TryParseResolveBy(attribute.ConstructorArguments[1], out var resolveBy))
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.InvalidResolveBy,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                implementationType.ToDisplayString()));
            return null;
        }

        var tryAdd = GetNamedBool(attribute, AttributeProperties.TryAdd, defaultValue: true);
        var enumerable = GetNamedBool(attribute, AttributeProperties.Enumerable, defaultValue: false);
        var key = GetNamedString(attribute, AttributeProperties.Key);
        var order = GetNamedUInt(attribute, AttributeProperties.Order, defaultValue: 0);

        return new RegistrationModel
        {
            ImplementationType = implementationType,
            ExplicitServiceType = null,
            ResolveBy = resolveBy,
            Lifetime = lifetime,
            TryAdd = tryAdd,
            Enumerable = enumerable,
            Key = key,
            Order = order,
            Location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
        };
    }

    private static RegistrationModel? CreateGenericRegistration(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        ICollection<Diagnostic> diagnostics)
    {
        if (attribute.ConstructorArguments.Length != 1)
        {
            return null;
        }

        if (!TryParseLifetime(attribute.ConstructorArguments[0], out var lifetime))
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.InvalidLifetime,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                attribute.ConstructorArguments[0].Value?.ToString() ?? "null",
                implementationType.ToDisplayString()));
            return null;
        }

        var attributeClass = attribute.AttributeClass;
        if (attributeClass?.TypeArguments.Length != 1 ||
            attributeClass.TypeArguments[0] is not INamedTypeSymbol explicitServiceType)
        {
            return null;
        }

        var tryAdd = GetNamedBool(attribute, AttributeProperties.TryAdd, defaultValue: true);
        var enumerable = GetNamedBool(attribute, AttributeProperties.Enumerable, defaultValue: false);
        var key = GetNamedString(attribute, AttributeProperties.Key);
        var order = GetNamedUInt(attribute, AttributeProperties.Order, defaultValue: 0);

        return new RegistrationModel
        {
            ImplementationType = implementationType,
            ExplicitServiceType = explicitServiceType,
            ResolveBy = RegistrationResolveBy.ExplicitService,
            Lifetime = lifetime,
            TryAdd = tryAdd,
            Enumerable = enumerable,
            Key = key,
            Order = order,
            Location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
        };
    }

    private static bool TryParseLifetime(TypedConstant value, out RegistrationLifetime lifetime)
    {
        lifetime = default;
        if (value.Value is null)
        {
            return false;
        }

        var numeric = Convert.ToInt32(value.Value);
        lifetime = numeric switch
        {
            1 => RegistrationLifetime.Singleton,
            2 => RegistrationLifetime.Scoped,
            3 => RegistrationLifetime.Transient,
            _ => default
        };
        return numeric is 1 or 2 or 3;
    }

    private static bool TryParseResolveBy(TypedConstant value, out RegistrationResolveBy resolveBy)
    {
        resolveBy = default;
        if (value.Value is null)
        {
            return false;
        }

        var numeric = Convert.ToInt32(value.Value);
        resolveBy = numeric switch
        {
            1 => RegistrationResolveBy.Self,
            2 => RegistrationResolveBy.ImplementedInterface,
            3 => RegistrationResolveBy.MatchingInterface,
            _ => default
        };
        return numeric is 1 or 2 or 3;
    }

    private static bool GetNamedBool(AttributeData attribute, string key, bool defaultValue)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == key && argument.Value.Value is bool boolValue)
            {
                return boolValue;
            }
        }

        return defaultValue;
    }

    private static string GetNamedString(AttributeData attribute, string key)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == key && argument.Value.Value is string stringValue)
            {
                return stringValue;
            }
        }

        return string.Empty;
    }

    private static uint GetNamedUInt(AttributeData attribute, string key, uint defaultValue)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == key && argument.Value.Value is uint uintValue)
            {
                return uintValue;
            }
        }

        return defaultValue;
    }

    private static IReadOnlyList<ServiceRegistrationDescriptor> ExpandRegistrations(
        IReadOnlyCollection<RegistrationModel> registrations,
        ICollection<Diagnostic> diagnostics)
    {
        var output = new List<ServiceRegistrationDescriptor>();

        foreach (var registration in registrations)
        {
            if (!registration.TryAdd && registration.Enumerable)
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.EnumerableRequiresTryAdd,
                    registration.Location,
                    registration.ImplementationType.ToDisplayString()));
                continue;
            }

            switch (registration.ResolveBy)
            {
                case RegistrationResolveBy.Self:
                    output.Add(ToDescriptor(
                        registration.ImplementationType,
                        registration.ImplementationType,
                        registration));
                    break;

                case RegistrationResolveBy.MatchingInterface:
                {
                    var interfaceName = $"{InterfaceNamePrefix}{registration.ImplementationType.Name}";
                    var matched = registration.ImplementationType.Interfaces
                        .FirstOrDefault(i => i.Name == interfaceName);
                    if (matched is null)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            GeneratorDiagnostics.MatchingInterfaceMissing,
                            registration.Location,
                            interfaceName,
                            registration.ImplementationType.ToDisplayString()));
                        continue;
                    }

                    output.Add(ToDescriptor(matched, registration.ImplementationType, registration));
                    break;
                }

                case RegistrationResolveBy.ImplementedInterface:
                    foreach (var interfaceType in registration.ImplementationType.Interfaces)
                    {
                        output.Add(ToDescriptor(interfaceType, registration.ImplementationType, registration));
                    }

                    break;

                case RegistrationResolveBy.ExplicitService:
                    if (registration.ExplicitServiceType is not null)
                    {
                        output.Add(ToDescriptor(registration.ExplicitServiceType, registration.ImplementationType,
                            registration));
                    }

                    break;

                default:
                    diagnostics.Add(Diagnostic.Create(
                        GeneratorDiagnostics.InvalidResolveBy,
                        registration.Location,
                        registration.ImplementationType.ToDisplayString()));
                    break;
            }
        }

        return output;
    }

    private static ServiceRegistrationDescriptor ToDescriptor(
        INamedTypeSymbol serviceType,
        INamedTypeSymbol implementationType,
        RegistrationModel registration)
    {
        return new ServiceRegistrationDescriptor
        {
            ServiceTypeName = serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ImplementationTypeName = implementationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Lifetime = registration.Lifetime,
            TryAdd = registration.TryAdd,
            Enumerable = registration.Enumerable,
            Key = registration.Key,
            Order = registration.Order,
            ImplementationNameSortKey = implementationType.Name
        };
    }

    private static string BuildSource(IReadOnlyCollection<ServiceRegistrationDescriptor> registrations,
        string? assemblyName)
    {
        var sanitisedAssemblyName = AssemblyNameSanitizer.Sanitize(assemblyName);
        var assemblySpecificMethodName = $"{GeneratedCode.AddServicesMethodNamePrefix}{sanitisedAssemblyName}";
        var registrationsSource = BuildRegistrationsSource(registrations);

        return $$"""
                 // <auto-generated />
                 using Microsoft.Extensions.DependencyInjection;
                 using Microsoft.Extensions.DependencyInjection.Extensions;

                 namespace {{GeneratedCode.Namespace}};

                 public static class {{GeneratedCode.ExtensionsClassName}}
                 {
                     internal static {{ServiceCollectionType}} {{GeneratedCode.AddServicesMethodName}}(
                         this {{ServiceCollectionType}} services)
                         => services.{{assemblySpecificMethodName}}();

                     public static {{ServiceCollectionType}} {{assemblySpecificMethodName}}(
                         this {{ServiceCollectionType}} services)
                     {
                 {{registrationsSource}}
                 {{Indent}}return services;
                     }
                 }

                 """;
    }

    private static string BuildRegistrationsSource(IReadOnlyCollection<ServiceRegistrationDescriptor> registrations)
    {
        var builder = new StringBuilder();
        foreach (var registration in registrations)
        {
            AppendRegistration(builder, registration);
        }

        return builder.ToString();
    }

    private static void AppendRegistration(StringBuilder builder, ServiceRegistrationDescriptor registration)
    {
        if (!string.IsNullOrEmpty(registration.Key))
        {
            AppendKeyedRegistration(builder, registration);
            return;
        }

        var serviceType = TypeOfExpression(registration.ServiceTypeName);
        var implType = TypeOfExpression(registration.ImplementationTypeName);

        if (registration is { TryAdd: true, Enumerable: true })
        {
            var lifetimeName = ToServiceLifetimeName(registration.Lifetime);
            builder.AppendLine(
                $"{Indent}services.{TryAddEnumerableMethod}({ServiceDescriptorType}.{DescribeMethod}({serviceType}, {implType}, {ServiceLifetimeType}.{lifetimeName}));");
            return;
        }

        var methodName = GetNonKeyedMethodName(registration.Lifetime, registration.TryAdd);
        builder.AppendLine($"{Indent}services.{methodName}({serviceType}, {implType});");
    }

    private static void AppendKeyedRegistration(StringBuilder builder, ServiceRegistrationDescriptor registration)
    {
        var keyLiteral = SymbolDisplay.FormatLiteral(registration.Key, quote: true);
        var methodName = GetKeyedMethodName(registration.Lifetime, registration.TryAdd);
        var serviceType = TypeOfExpression(registration.ServiceTypeName);
        var implType = TypeOfExpression(registration.ImplementationTypeName);

        builder.AppendLine($"{Indent}services.{methodName}({serviceType}, {keyLiteral}, {implType});");
    }

    private static string TypeOfExpression(string typeName)
        => $"{TypeOfPrefix}{typeName}{TypeOfSuffix}";

    private static string GetNonKeyedMethodName(RegistrationLifetime lifetime, bool tryAdd)
    {
        return (lifetime, tryAdd) switch
        {
            (RegistrationLifetime.Singleton, true) => Methods.TryAddSingleton,
            (RegistrationLifetime.Scoped, true) => Methods.TryAddScoped,
            (RegistrationLifetime.Transient, true) => Methods.TryAddTransient,
            (RegistrationLifetime.Singleton, false) => Methods.AddSingleton,
            (RegistrationLifetime.Scoped, false) => Methods.AddScoped,
            (RegistrationLifetime.Transient, false) => Methods.AddTransient,
            _ => throw new InvalidOperationException(UnsupportedLifetimeMessage)
        };
    }

    private static string GetKeyedMethodName(RegistrationLifetime lifetime, bool tryAdd)
    {
        return (lifetime, tryAdd) switch
        {
            (RegistrationLifetime.Singleton, true) => Methods.TryAddKeyedSingleton,
            (RegistrationLifetime.Scoped, true) => Methods.TryAddKeyedScoped,
            (RegistrationLifetime.Transient, true) => Methods.TryAddKeyedTransient,
            (RegistrationLifetime.Singleton, false) => Methods.AddKeyedSingleton,
            (RegistrationLifetime.Scoped, false) => Methods.AddKeyedScoped,
            (RegistrationLifetime.Transient, false) => Methods.AddKeyedTransient,
            _ => throw new InvalidOperationException(UnsupportedLifetimeMessage)
        };
    }

    private static string ToServiceLifetimeName(RegistrationLifetime lifetime)
    {
        return lifetime switch
        {
            RegistrationLifetime.Singleton => Lifetimes.Singleton,
            RegistrationLifetime.Scoped => Lifetimes.Scoped,
            RegistrationLifetime.Transient => Lifetimes.Transient,
            _ => throw new InvalidOperationException(UnsupportedLifetimeMessage)
        };
    }
}