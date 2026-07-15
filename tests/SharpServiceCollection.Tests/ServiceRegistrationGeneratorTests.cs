using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Generators;
using Xunit;

namespace SharpServiceCollection.Tests;

public sealed class ServiceRegistrationGeneratorTests
{
    [Fact]
    public void Root_CallsHostAggregatorMethods_NotInlineTypes()
    {
        var (aggregator, root) = RunRootGenerator(
            assemblyName: "Api.Host",
            source: """
                using System.Threading.Tasks;
                using Microsoft.Extensions.DependencyInjection;
                using SharpServiceCollection.Attributes;
                using SharpServiceCollection.Interfaces;

                namespace Api.Host.ServiceRegistrations;

                [ServiceRegistrationItem]
                public sealed class ConfigureApiSecurity : IServiceRegistration
                {
                    public Task RegisterAsync(IServiceCollection services) => Task.CompletedTask;
                }

                [ServiceRegistrationItem(Order = 1)]
                public sealed class ConfigureOpenTelemetry : IServiceRegistration
                {
                    public Task RegisterAsync(IServiceCollection services) => Task.CompletedTask;
                }
                """,
            descending: true);

        Assert.Contains(
            "ServiceRegistrationAggregator_Api_Host",
            aggregator,
            StringComparison.Ordinal);
        Assert.Contains("RegisterAsync_0_0", aggregator, StringComparison.Ordinal);
        Assert.Contains("RegisterAsync_1_1", aggregator, StringComparison.Ordinal);

        Assert.Contains(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_1_1(services)",
            root,
            StringComparison.Ordinal);
        Assert.Contains(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_0_0(services)",
            root,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "new global::Api.Host.ServiceRegistrations.ConfigureOpenTelemetry()",
            root,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "new global::Api.Host.ServiceRegistrations.ConfigureApiSecurity()",
            root,
            StringComparison.Ordinal);

        var orderOne = root.IndexOf(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_1_1",
            StringComparison.Ordinal);
        var orderZero = root.IndexOf(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_0_0",
            StringComparison.Ordinal);
        Assert.True(orderOne >= 0 && orderZero >= 0);
        Assert.True(orderOne < orderZero, "Descending Order should call Order=1 before Order=0");
    }

    [Fact]
    public void Root_SortsHostAggregatorWithReferencedModuleByOrder()
    {
        var moduleReference = CompileModuleAggregator(
            """
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using SharpServiceCollection.Attributes;

            namespace SharpServiceCollection.Generated;

            [ServiceRegistrationAggregator]
            public static class ServiceRegistrationAggregator_Common_Application
            {
                public static async Task<IServiceCollection> RegisterAsync_9000_0(IServiceCollection services)
                {
                    await Task.CompletedTask;
                    return services;
                }

                public static async Task<IServiceCollection> RegisterAsync_0_1(IServiceCollection services)
                {
                    await Task.CompletedTask;
                    return services;
                }
            }
            """);

        var (_, root) = RunRootGenerator(
            assemblyName: "Api.Host",
            source: """
                using System.Threading.Tasks;
                using Microsoft.Extensions.DependencyInjection;
                using SharpServiceCollection.Attributes;
                using SharpServiceCollection.Interfaces;

                namespace Api.Host.ServiceRegistrations;

                [ServiceRegistrationItem]
                public sealed class ConfigureMvc : IServiceRegistration
                {
                    public Task RegisterAsync(IServiceCollection services) => Task.CompletedTask;
                }

                [ServiceRegistrationItem(Order = 1)]
                public sealed class ConfigureOpenTelemetry : IServiceRegistration
                {
                    public Task RegisterAsync(IServiceCollection services) => Task.CompletedTask;
                }
                """,
            descending: true,
            additionalReferences: [moduleReference]);

        Assert.DoesNotContain("new global::Api.Host", root, StringComparison.Ordinal);

        var highModule = root.IndexOf(
            "ServiceRegistrationAggregator_Common_Application.RegisterAsync_9000_0",
            StringComparison.Ordinal);
        var hostOrderOne = root.IndexOf(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_1_1",
            StringComparison.Ordinal);
        var hostOrderZero = root.IndexOf(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_0_0",
            StringComparison.Ordinal);
        var lowModule = root.IndexOf(
            "ServiceRegistrationAggregator_Common_Application.RegisterAsync_0_1",
            StringComparison.Ordinal);

        Assert.True(highModule >= 0 && hostOrderOne >= 0 && hostOrderZero >= 0 && lowModule >= 0);
        Assert.True(highModule < hostOrderOne);
        Assert.True(hostOrderOne < hostOrderZero);
        Assert.True(hostOrderZero < lowModule);
    }

    private static (string Aggregator, string Root) RunRootGenerator(
        string assemblyName,
        string source,
        bool descending,
        IEnumerable<MetadataReference>? additionalReferences = null)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: GetReferences(additionalReferences),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var options = new Dictionary<string, string>
        {
            ["build_property.ServiceRegistrationRoot"] = "true",
            ["build_property.ServiceRegistrationRootDescSortOrder"] = descending ? "true" : "false"
        };

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new ServiceRegistrationGenerator().AsSourceGenerator()],
            optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        var runResult = driver.GetRunResult();

        var aggregator = Assert.Single(
            runResult.GeneratedTrees.Where(tree =>
                tree.FilePath.EndsWith("SharpServiceCollection.ServiceRegistration.Aggregator.g.cs",
                    StringComparison.Ordinal)));
        var root = Assert.Single(
            runResult.GeneratedTrees.Where(tree =>
                tree.FilePath.EndsWith("SharpServiceCollection.ServiceRegistration.g.cs",
                    StringComparison.Ordinal)));

        return (aggregator.GetText().ToString(), root.GetText().ToString());
    }

    private static MetadataReference CompileModuleAggregator(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "Common.Application",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: GetReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        Assert.True(
            emitResult.Success,
            string.Join(Environment.NewLine, emitResult.Diagnostics.Select(d => d.ToString())));

        stream.Position = 0;
        return MetadataReference.CreateFromStream(stream);
    }

    private static ImmutableArray<MetadataReference> GetReferences(
        IEnumerable<MetadataReference>? additional = null)
    {
        var paths = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        var references = paths
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(MetadataReference (path) => MetadataReference.CreateFromFile(path))
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(ServiceRegistrationItemAttribute).Assembly.Location));

        if (additional is not null)
        {
            references.AddRange(additional);
        }

        return [..references];
    }

    private sealed class TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
        : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _options = new TestAnalyzerConfigOptions(globalOptions);

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    private sealed class TestAnalyzerConfigOptions(Dictionary<string, string> options) : AnalyzerConfigOptions
    {
        private readonly ReadOnlyDictionary<string, string> _options = new(options);

        public override bool TryGetValue(string key, out string value) =>
            _options.TryGetValue(key, out value!);
    }
}
