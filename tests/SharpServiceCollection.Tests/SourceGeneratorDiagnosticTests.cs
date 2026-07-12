using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpServiceCollection.Generators;
using Xunit;

namespace SharpServiceCollection.Tests;

public sealed class SourceGeneratorDiagnosticTests
{
    [Fact]
    public void EnumerableRequiresTryAdd_ReportsError()
    {
        var diagnostics = Run<InjectableDependencyGenerator>("""
            using SharpServiceCollection.Attributes;
            using SharpServiceCollection.Enums;

            [InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self, Enumerable = true, TryAdd = false)]
            public class Service;
            """);

        AssertDiagnostic(diagnostics, "SSC001", "Enumerable=true requires TryAdd=true");
    }

    [Fact]
    public void MatchingInterfaceMissing_ReportsError()
    {
        var diagnostics = Run<InjectableDependencyGenerator>("""
            using SharpServiceCollection.Attributes;
            using SharpServiceCollection.Enums;

            [InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface)]
            public class Service;
            """);

        AssertDiagnostic(diagnostics, "SSC002", "requires interface 'IService'");
    }

    [Fact]
    public void InvalidLifetime_ReportsError()
    {
        var diagnostics = Run<InjectableDependencyGenerator>("""
            using SharpServiceCollection.Attributes;
            using SharpServiceCollection.Enums;

            [InjectableDependency((InstanceLifetime)99, ResolveBy.Self)]
            public class Service;
            """);

        AssertDiagnostic(diagnostics, "SSC003", "Unsupported lifetime");
    }

    [Fact]
    public void InvalidResolveBy_ReportsError()
    {
        var diagnostics = Run<InjectableDependencyGenerator>("""
            using SharpServiceCollection.Attributes;
            using SharpServiceCollection.Enums;

            [InjectableDependency(InstanceLifetime.Scoped, (ResolveBy)99)]
            public class Service;
            """);

        AssertDiagnostic(diagnostics, "SSC004", "Unsupported resolve strategy");
    }

    [Fact]
    public void ServiceRegistrationMustBeSealed_ReportsError()
    {
        var diagnostics = Run<ServiceRegistrationGenerator>("""
            using SharpServiceCollection.Attributes;
            using SharpServiceCollection.Interfaces;

            [ServiceRegistrationItem]
            public class Service : IServiceRegistration;
            """);

        AssertDiagnostic(diagnostics, "SSC005", "but is not sealed");
    }

    [Fact]
    public void ServiceRegistrationMustImplementInterface_ReportsError()
    {
        var diagnostics = Run<ServiceRegistrationGenerator>("""
            using SharpServiceCollection.Attributes;

            [ServiceRegistrationItem]
            public sealed class Service;
            """);

        AssertDiagnostic(diagnostics, "SSC006", "must implement IServiceRegistration");
    }

    private static ImmutableArray<Diagnostic> Run<TGenerator>(string source)
        where TGenerator : IIncrementalGenerator, new()
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "DiagnosticTestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: GetPlatformReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out _,
            out _);

        return [
            ..driver.GetRunResult()
                .Results
                .SelectMany(result => result.Diagnostics)
        ];
    }

    private static ImmutableArray<MetadataReference> GetPlatformReferences()
    {
        var paths = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        return [
            ..paths
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(MetadataReference (path) => MetadataReference.CreateFromFile(path))
        ];
    }

    private static void AssertDiagnostic(
        ImmutableArray<Diagnostic> diagnostics,
        string diagnosticId,
        string messageFragment)
    {
        var diagnostic = Assert.Single(diagnostics.Where(item => item.Id == diagnosticId));
        Assert.Contains(messageFragment, diagnostic.GetMessage());
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }
}
