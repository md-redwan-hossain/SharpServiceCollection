using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SharpServiceCollection.SourceGenerator.InternalTypes;
using static SharpServiceCollection.SourceGenerator.InternalTypes.GeneratorConstants;
using static SharpServiceCollection.SourceGenerator.InternalTypes.GeneratorConstants.DependencyInjection;

namespace SharpServiceCollection.SourceGenerator.Generators;

[Generator]
public sealed class InjectableDependencyGenerator : IIncrementalGenerator
{
    private const string Indent = "        ";
    private const string TypeOfPrefix = "typeof(";
    private const string TypeOfSuffix = ")";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidate(node),
                transform: static (ctx, _) => GetTypeSymbol(ctx))
            .SelectMany(static (symbol, _) => symbol is null ? [] : ImmutableArray.Create(symbol));

        var compilationAndTypes = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndTypes,
            static (spc, source) => { EmitGeneratedCode(spc, source.Right); });
    }

    private static bool IsCandidate(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    private static INamedTypeSymbol? GetTypeSymbol(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        return context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
    }

    private static void EmitGeneratedCode(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> types)
    {
        var registrations = new List<RegistrationModel>();
        var diagnostics = new List<Diagnostic>();
        var processed = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var type in types)
        {
            if (!processed.Add(type))
            {
                continue;
            }

            CollectRegistrations(type, registrations, diagnostics);
        }

        var expanded = ExpandRegistrations(registrations, diagnostics);
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        var sorted = expanded
            .OrderBy(r => r.Order)
            .ThenBy(r => r.ImplementationNameSortKey, StringComparer.Ordinal)
            .ToList();

        var generatedSource = BuildSource(sorted);
        context.AddSource(GeneratedFileName, SourceText.From(generatedSource, Encoding.UTF8));
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

    private static string BuildSource(IReadOnlyCollection<ServiceRegistrationDescriptor> registrations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection.Extensions;");
        builder.AppendLine();
        builder.AppendLine($"namespace {GeneratedCode.Namespace};");
        builder.AppendLine();
        builder.AppendLine($"public static class {GeneratedCode.ExtensionsClassName}");
        builder.AppendLine("{");
        builder.AppendLine(
            $"    public static {ServiceCollectionType} {GeneratedCode.AddServicesMethodName}(");
        builder.AppendLine($"        this {ServiceCollectionType} services)");
        builder.AppendLine("    {");

        foreach (var registration in registrations)
        {
            AppendRegistration(builder, registration);
        }

        builder.AppendLine($"{Indent}return services;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendRegistration(StringBuilder builder, ServiceRegistrationDescriptor registration)
    {
        if (!string.IsNullOrEmpty(registration.Key))
        {
            AppendKeyedRegistration(builder, registration);
            return;
        }

        if (registration is { TryAdd: true, Enumerable: true })
        {
            var lifetimeName = ToServiceLifetimeName(registration.Lifetime);
            builder.Append(
                $"{Indent}services.{TryAddEnumerableMethod}({ServiceDescriptorType}.{DescribeMethod}(");
            AppendTypeOf(builder, registration.ServiceTypeName);
            builder.Append(", ");
            AppendTypeOf(builder, registration.ImplementationTypeName);
            builder.Append($", {ServiceLifetimeType}.");
            builder.Append(lifetimeName);
            builder.AppendLine("));");
            return;
        }

        var methodName = GetNonKeyedMethodName(registration.Lifetime, registration.TryAdd);
        builder.Append($"{Indent}services.{methodName}(");
        AppendTypeOf(builder, registration.ServiceTypeName);
        builder.Append(", ");
        AppendTypeOf(builder, registration.ImplementationTypeName);
        builder.AppendLine(");");
    }

    private static void AppendKeyedRegistration(StringBuilder builder, ServiceRegistrationDescriptor registration)
    {
        var keyLiteral = registration.Key.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var methodName = GetKeyedMethodName(registration.Lifetime, registration.TryAdd);

        builder.Append($"{Indent}services.{methodName}(");
        AppendTypeOf(builder, registration.ServiceTypeName);
        builder.Append($", \"{keyLiteral}\", ");
        AppendTypeOf(builder, registration.ImplementationTypeName);
        builder.AppendLine(");");
    }

    private static void AppendTypeOf(StringBuilder builder, string typeName)
    {
        builder.Append(TypeOfPrefix);
        builder.Append(typeName);
        builder.Append(TypeOfSuffix);
    }

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
