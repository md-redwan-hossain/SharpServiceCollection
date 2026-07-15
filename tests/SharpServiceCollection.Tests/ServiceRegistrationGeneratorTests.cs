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

                [ServiceRegistrationItem(Priority = 1)]
                public sealed class ConfigureOpenTelemetry : IServiceRegistration
                {
                    public Task RegisterAsync(IServiceCollection services) => Task.CompletedTask;
                }
                """);

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

        var priorityOne = root.IndexOf(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_1_1",
            StringComparison.Ordinal);
        var priorityZero = root.IndexOf(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_0_0",
            StringComparison.Ordinal);
        Assert.True(priorityOne >= 0 && priorityZero >= 0);
        Assert.True(priorityOne < priorityZero, "Higher Priority should run before lower Priority");
    }

    [Fact]
    public void Root_SortsHostAggregatorWithReferencedModuleByPriority()
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

                [ServiceRegistrationItem(Priority = 1)]
                public sealed class ConfigureOpenTelemetry : IServiceRegistration
                {
                    public Task RegisterAsync(IServiceCollection services) => Task.CompletedTask;
                }
                """,
            additionalReferences: [moduleReference]);

        Assert.DoesNotContain("new global::Api.Host", root, StringComparison.Ordinal);

        var highModule = root.IndexOf(
            "ServiceRegistrationAggregator_Common_Application.RegisterAsync_9000_0",
            StringComparison.Ordinal);
        var hostPriorityOne = root.IndexOf(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_1_1",
            StringComparison.Ordinal);
        var hostPriorityZero = root.IndexOf(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_0_0",
            StringComparison.Ordinal);
        var lowModule = root.IndexOf(
            "ServiceRegistrationAggregator_Common_Application.RegisterAsync_0_1",
            StringComparison.Ordinal);

        Assert.True(highModule >= 0 && hostPriorityOne >= 0 && hostPriorityZero >= 0 && lowModule >= 0);
        Assert.True(highModule < hostPriorityOne);
        Assert.True(hostPriorityOne < hostPriorityZero);
        Assert.True(hostPriorityZero < lowModule);
    }

    [Fact]
    public void Root_NegativePriority_RunsAfterZero_EncodedAsNPrefix()
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
                public sealed class ConfigureMvc : IServiceRegistration
                {
                    public Task RegisterAsync(IServiceCollection services) => Task.CompletedTask;
                }

                [ServiceRegistrationItem(Priority = -5)]
                public sealed class ConfigureLate : IServiceRegistration
                {
                    public Task RegisterAsync(IServiceCollection services) => Task.CompletedTask;
                }
                """);

        Assert.Contains("RegisterAsync_neg5_0", aggregator, StringComparison.Ordinal);
        Assert.Contains("RegisterAsync_0_1", aggregator, StringComparison.Ordinal);

        var zero = root.IndexOf(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_0_1",
            StringComparison.Ordinal);
        var negative = root.IndexOf(
            "ServiceRegistrationAggregator_Api_Host.RegisterAsync_neg5_0",
            StringComparison.Ordinal);
        Assert.True(zero >= 0 && negative >= 0);
        Assert.True(zero < negative, "Priority 0 should run before Priority -5");
    }

    private static (string Aggregator, string Root) RunRootGenerator(
        string assemblyName,
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: GetReferences(additionalReferences),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var options = new Dictionary<string, string>
        {
            ["build_property.ServiceRegistrationRoot"] = "true"
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
