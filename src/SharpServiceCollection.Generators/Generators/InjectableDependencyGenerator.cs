using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SharpServiceCollection.Constants;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Helpers;
using SharpServiceCollection.InternalTypes;

namespace SharpServiceCollection.Generators;

[Generator]
public sealed class InjectableDependencyGenerator : IIncrementalGenerator
{
    private const string RuntimeAssemblyName = "SharpServiceCollection";
    private const string DisablePropertyName =
        "build_property.DisableInjectableDependencyGenerator";
    private const string GeneratedFileName = "SharpServiceCollection.InjectableDependency.Generated.g.cs";
    private const string UnsupportedLifetimeMessage = "Unsupported lifetime";
    private const string InterfaceNamePrefix = "I";
    private const string Indent = "        ";
    private const string TypeOfPrefix = "typeof(";
    private const string TypeOfSuffix = ")";

    private const string NonGenericAttributeMetadataName =
        "SharpServiceCollection.Attributes.InjectableDependencyAttribute";

    private const string GenericAttributeMetadataName =
        "SharpServiceCollection.Attributes.InjectableDependencyAttribute`1";

    private static class TrackingNames
    {
        internal const string NonGeneric = "InjectableDependency.NonGeneric";
        internal const string Generic = "InjectableDependency.Generic";
        internal const string CollectNonGeneric = "InjectableDependency.CollectNonGeneric";
        internal const string CollectGeneric = "InjectableDependency.CollectGeneric";
        internal const string CombineStreams = "InjectableDependency.CombineStreams";
        internal const string CombineAssembly = "InjectableDependency.CombineAssembly";
    }

    private static class AttributeMetadata
    {
        internal const string Name = "InjectableDependencyAttribute";
        internal const string Namespace = "SharpServiceCollection.Attributes";
    }

    private static class AttributeProperties
    {
        internal const string TryAdd = "TryAdd";
        internal const string Enumerable = "Enumerable";
        internal const string Key = "Key";
        internal const string Order = "Order";
    }

    private static class GeneratedCode
    {
        internal const string Namespace = "SharpServiceCollection.Generated";
        internal const string ExtensionsClassName = "GeneratedServiceCollectionExtensions";
        internal const string AddServicesMethodName = "AddAttributedServices";
        internal const string AddServicesMethodNamePrefix = "AddAttributedServicesFrom_";
    }

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

    private static readonly DiagnosticDescriptor EnumerableRequiresTryAdd = new(
        id: DiagnosticIds.EnumerableRequiresTryAdd,
        title: EnumerableRequiresTryAddTitle,
        messageFormat: "Enumerable=true requires TryAdd=true for '{0}'",
        category: SharedConsts.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: EnumerableRequiresTryAddDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-service-registration"));

    private static readonly DiagnosticDescriptor MatchingInterfaceMissing = new(
        id: DiagnosticIds.MatchingInterfaceMissing,
        title: MatchingInterfaceMissingTitle,
        messageFormat: "ResolveBy.MatchingInterface requires interface '{0}' on '{1}'",
        category: SharedConsts.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: MatchingInterfaceMissingDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-service-registration"));

    private static readonly DiagnosticDescriptor InvalidLifetime = new(
        id: DiagnosticIds.InvalidLifetime,
        title: InvalidLifetimeTitle,
        messageFormat: "Unsupported lifetime '{0}' on '{1}'",
        category: SharedConsts.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: InvalidLifetimeDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-service-registration"));

    private static readonly DiagnosticDescriptor InvalidResolveBy = new(
        id: DiagnosticIds.InvalidResolveBy,
        title: InvalidResolveByTitle,
        messageFormat: "Unsupported resolve strategy on '{0}'",
        category: SharedConsts.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: InvalidResolveByDescription,
        helpLinkUri: string.Format(HelpLinkUriFormat, "source-generated-service-registration"));

    private static class DependencyInjection
    {
        internal const string ServiceCollectionType =
            "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";

        internal const string ServiceDescriptorType =
            "global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor";

        internal const string ServiceLifetimeType =
            "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime";

        internal const string TryAddEnumerableMethod = "TryAddEnumerable";
        internal const string DescribeMethod = "Describe";

        internal static class Methods
        {
            internal const string TryAddSingleton = "TryAddSingleton";
            internal const string TryAddScoped = "TryAddScoped";
            internal const string TryAddTransient = "TryAddTransient";
            internal const string AddSingleton = "AddSingleton";
            internal const string AddScoped = "AddScoped";
            internal const string AddTransient = "AddTransient";
            internal const string TryAddKeyedSingleton = "TryAddKeyedSingleton";
            internal const string TryAddKeyedScoped = "TryAddKeyedScoped";
            internal const string TryAddKeyedTransient = "TryAddKeyedTransient";
            internal const string AddKeyedSingleton = "AddKeyedSingleton";
            internal const string AddKeyedScoped = "AddKeyedScoped";
            internal const string AddKeyedTransient = "AddKeyedTransient";
        }

        internal static class Lifetimes
        {
            internal const string Singleton = nameof(InstanceLifetime.Singleton);
            internal const string Scoped = nameof(InstanceLifetime.Scoped);
            internal const string Transient = nameof(InstanceLifetime.Transient);
        }
    }



    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var isDisabled = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
                provider.GlobalOptions.TryGetValue(DisablePropertyName, out var value) &&
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        var nonGenericStream = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                NonGenericAttributeMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => AnalyzeClass(ctx))
            .WithTrackingName(TrackingNames.NonGeneric);

        var genericStream = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenericAttributeMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => AnalyzeClass(ctx))
            .WithTrackingName(TrackingNames.Generic);


        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);

        var combined = nonGenericStream
            .Collect()
            .WithTrackingName(TrackingNames.CollectNonGeneric)
            .Combine(genericStream.Collect().WithTrackingName(TrackingNames.CollectGeneric))
            .WithTrackingName(TrackingNames.CombineStreams)
            .Combine(assemblyName)
            .WithTrackingName(TrackingNames.CombineAssembly)
            .Combine(isDisabled);

        context.RegisterSourceOutput(combined,
            static (spc, source) =>
            {
                if (source.Right)
                {
                    return;
                }

                EmitGeneratedCode(
                    spc,
                    source.Left.Left.Left,
                    source.Left.Left.Right,
                    source.Left.Right);
            });
    }

    private static TypeRegistrationResult AnalyzeClass(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol symbol)
        {
            return new TypeRegistrationResult { Descriptors = [], Diagnostics = [] };
        }

        var registrations = new List<RegistrationModel>();
        var diagnostics = new List<Diagnostic>();
        CollectRegistrations(ctx.Attributes, symbol, registrations, diagnostics);

        var descriptors = ExpandRegistrations(registrations, diagnostics);

        if (descriptors.Count == 0 && diagnostics.Count == 0)
        {
            return new TypeRegistrationResult { Descriptors = [], Diagnostics = [] };
        }

        return new TypeRegistrationResult
        {
            Descriptors = [..descriptors],
            Diagnostics = [..diagnostics]
        };
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
            InstanceLifetime Lifetime, bool TryAdd, bool Enumerable)>();

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
            InstanceLifetime Lifetime, bool TryAdd, bool Enumerable)> seen,
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
        ImmutableArray<AttributeData> attributes,
        INamedTypeSymbol typeSymbol,
        ICollection<RegistrationModel> registrations,
        ICollection<Diagnostic> diagnostics)
    {
        foreach (var attribute in attributes)
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
                    registrations.Add(registration.Value);
                }
            }
            else if (IsGenericInjectableDependencyAttribute(attributeClass))
            {
                var registration = CreateGenericRegistration(typeSymbol, attribute, diagnostics);
                if (registration is not null)
                {
                    registrations.Add(registration.Value);
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
                InvalidLifetime,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                attribute.ConstructorArguments[0].Value?.ToString() ?? "null",
                implementationType.ToDisplayString()));
            return null;
        }

        if (!TryParseResolveBy(attribute.ConstructorArguments[1], out var resolveBy))
        {
            diagnostics.Add(Diagnostic.Create(
                InvalidResolveBy,
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
                InvalidLifetime,
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
            ResolveBy = default,
            Lifetime = lifetime,
            TryAdd = tryAdd,
            Enumerable = enumerable,
            Key = key,
            Order = order,
            Location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
        };
    }

    private static bool TryParseLifetime(TypedConstant value, out InstanceLifetime lifetime)
    {
        lifetime = default;
        if (value.Value is not byte numeric ||
            numeric is not ((byte)InstanceLifetime.Singleton or
                (byte)InstanceLifetime.Scoped or
                (byte)InstanceLifetime.Transient))
        {
            return false;
        }

        lifetime = (InstanceLifetime)numeric;
        return true;
    }

    private static bool TryParseResolveBy(TypedConstant value, out ResolveBy resolveBy)
    {
        resolveBy = default;
        if (value.Value is not byte numeric ||
            numeric is not ((byte)ResolveBy.Self or
                (byte)ResolveBy.ImplementedInterface or
                (byte)ResolveBy.MatchingInterface))
        {
            return false;
        }

        resolveBy = (ResolveBy)numeric;
        return true;
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
            if (registration is { TryAdd: false, Enumerable: true })
            {
                diagnostics.Add(Diagnostic.Create(
                    EnumerableRequiresTryAdd,
                    registration.Location,
                    registration.ImplementationType.ToDisplayString()));
                continue;
            }

            if (registration.ExplicitServiceType is not null)
            {
                output.Add(ToDescriptor(registration.ExplicitServiceType, registration.ImplementationType,
                    registration));
                continue;
            }

            switch (registration.ResolveBy)
            {
                case ResolveBy.Self:
                    output.Add(ToDescriptor(
                        registration.ImplementationType,
                        registration.ImplementationType,
                        registration));
                    break;

                case ResolveBy.MatchingInterface:
                {
                    var interfaceName = $"{InterfaceNamePrefix}{registration.ImplementationType.Name}";
                    var matched = registration.ImplementationType.Interfaces
                        .FirstOrDefault(i => i.Name == interfaceName);
                    if (matched is null)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            MatchingInterfaceMissing,
                            registration.Location,
                            interfaceName,
                            registration.ImplementationType.ToDisplayString()));
                        continue;
                    }

                    output.Add(ToDescriptor(matched, registration.ImplementationType, registration));
                    break;
                }

                case ResolveBy.ImplementedInterface:
                    foreach (var interfaceType in registration.ImplementationType.Interfaces)
                    {
                        output.Add(ToDescriptor(interfaceType, registration.ImplementationType, registration));
                    }

                    break;


                default:
                    diagnostics.Add(Diagnostic.Create(
                        InvalidResolveBy,
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
        var sanitisedAssemblyName = AssemblyNameSanitizer.Sanitize(assemblyName).TrimStart('_');
        var assemblySpecificMethodName = $"{GeneratedCode.AddServicesMethodNamePrefix}{sanitisedAssemblyName}";
        var registrationsSource = BuildRegistrationsSource(registrations);

        return $$"""
                 // <auto-generated />
                 using Microsoft.Extensions.DependencyInjection;
                 using Microsoft.Extensions.DependencyInjection.Extensions;

                 namespace {{GeneratedCode.Namespace}};

                 public static class {{GeneratedCode.ExtensionsClassName}}
                 {
                     internal static {{DependencyInjection.ServiceCollectionType}} {{GeneratedCode.AddServicesMethodName}}(
                                              this {{DependencyInjection.ServiceCollectionType}} services)
                         => services.{{assemblySpecificMethodName}}();

                     public static {{DependencyInjection.ServiceCollectionType}} {{assemblySpecificMethodName}}(
                                              this {{DependencyInjection.ServiceCollectionType}} services)
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
                $"{Indent}services.{DependencyInjection.TryAddEnumerableMethod}({DependencyInjection.ServiceDescriptorType}.{DependencyInjection.DescribeMethod}({serviceType}, {implType}, {DependencyInjection.ServiceLifetimeType}.{lifetimeName}));");
            return;
        }

        var methodName = GetNonKeyedMethodName(registration.Lifetime, registration.TryAdd);
        var arguments = registration.ServiceTypeName == registration.ImplementationTypeName
            ? serviceType
            : $"{serviceType}, {implType}";
        builder.AppendLine($"{Indent}services.{methodName}({arguments});");
    }

    private static void AppendKeyedRegistration(StringBuilder builder, ServiceRegistrationDescriptor registration)
    {
        var keyLiteral = SymbolDisplay.FormatLiteral(registration.Key, quote: true);
        var methodName = GetKeyedMethodName(registration.Lifetime, registration.TryAdd);
        var serviceType = TypeOfExpression(registration.ServiceTypeName);
        var implType = TypeOfExpression(registration.ImplementationTypeName);
        var arguments = registration.ServiceTypeName == registration.ImplementationTypeName
            ? $"{serviceType}, {keyLiteral}"
            : $"{serviceType}, {keyLiteral}, {implType}";

        builder.AppendLine($"{Indent}services.{methodName}({arguments});");
    }

    private static string TypeOfExpression(string typeName)
        => $"{TypeOfPrefix}{typeName}{TypeOfSuffix}";

    private static string GetNonKeyedMethodName(InstanceLifetime lifetime, bool tryAdd)
    {
        return (lifetime, tryAdd) switch
        {
            (InstanceLifetime.Singleton, true) => DependencyInjection.Methods.TryAddSingleton,
            (InstanceLifetime.Scoped, true) => DependencyInjection.Methods.TryAddScoped,
            (InstanceLifetime.Transient, true) => DependencyInjection.Methods.TryAddTransient,
            (InstanceLifetime.Singleton, false) => DependencyInjection.Methods.AddSingleton,
            (InstanceLifetime.Scoped, false) => DependencyInjection.Methods.AddScoped,
            (InstanceLifetime.Transient, false) => DependencyInjection.Methods.AddTransient,
            _ => throw new InvalidOperationException(UnsupportedLifetimeMessage)
        };
    }

    private static string GetKeyedMethodName(InstanceLifetime lifetime, bool tryAdd)
    {
        return (lifetime, tryAdd) switch
        {
            (InstanceLifetime.Singleton, true) => DependencyInjection.Methods.TryAddKeyedSingleton,
            (InstanceLifetime.Scoped, true) => DependencyInjection.Methods.TryAddKeyedScoped,
            (InstanceLifetime.Transient, true) => DependencyInjection.Methods.TryAddKeyedTransient,
            (InstanceLifetime.Singleton, false) => DependencyInjection.Methods.AddKeyedSingleton,
            (InstanceLifetime.Scoped, false) => DependencyInjection.Methods.AddKeyedScoped,
            (InstanceLifetime.Transient, false) => DependencyInjection.Methods.AddKeyedTransient,
            _ => throw new InvalidOperationException(UnsupportedLifetimeMessage)
        };
    }

    private static string ToServiceLifetimeName(InstanceLifetime lifetime)
    {
        return lifetime switch
        {
            InstanceLifetime.Singleton => DependencyInjection.Lifetimes.Singleton,
            InstanceLifetime.Scoped => DependencyInjection.Lifetimes.Scoped,
            InstanceLifetime.Transient => DependencyInjection.Lifetimes.Transient,
            _ => throw new InvalidOperationException(UnsupportedLifetimeMessage)
        };
    }
}