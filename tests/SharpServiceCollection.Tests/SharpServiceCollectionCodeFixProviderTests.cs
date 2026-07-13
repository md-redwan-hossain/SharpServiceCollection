using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace SharpServiceCollection.Tests;

public sealed class SharpServiceCollectionCodeFixProviderTests
{
    [Fact]
    public async Task EnumerableDiagnostic_SuggestsSettingTryAddTrue()
    {
        const string code =
            "[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self, Enumerable = true, TryAdd = false)] class Service;";

        var fixedCode = await ApplyFixAsync(code, "SSC001", "Set TryAdd to true",
            code.IndexOf('['));

        Assert.Contains("TryAdd = true", fixedCode);
    }

    [Fact]
    public async Task EnumerableDiagnostic_SuggestsRemovingTryAdd()
    {
        const string code =
            "[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self, Enumerable = true, TryAdd = false)] class Service;";

        var fixedCode =
            await ApplyFixAsync(code, "SSC001", "Remove TryAdd", code.IndexOf('['));

        Assert.DoesNotContain("TryAdd", fixedCode);
    }

    [Fact]
    public async Task MatchingInterfaceDiagnostic_SuggestsResolvingBySelf()
    {
        const string code =
            "[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface)] class Service;";

        var fixedCode = await ApplyFixAsync(code, "SSC002", "Resolve by implementation type",
            code.IndexOf('['));

        Assert.Contains("using SharpServiceCollection.Enums;", fixedCode);
        Assert.Contains("ResolveBy.Self", fixedCode);
        Assert.DoesNotContain("global::SharpServiceCollection.Enums", fixedCode);
    }

    [Fact]
    public async Task InvalidLifetimeDiagnostic_SuggestsValidLifetime()
    {
        const string code = "[InjectableDependency((InstanceLifetime)99, ResolveBy.Self)] class Service;";

        var fixedCode = await ApplyFixAsync(code, "SSC003", "Use Scoped", code.IndexOf('['));

        Assert.Contains("using SharpServiceCollection.Enums;", fixedCode);
        Assert.Contains("InstanceLifetime.Scoped", fixedCode);
        Assert.DoesNotContain("global::SharpServiceCollection.Enums", fixedCode);
    }

    [Fact]
    public async Task InvalidResolveByDiagnostic_SuggestsValidResolveStrategy()
    {
        const string code = "[InjectableDependency(InstanceLifetime.Scoped, (ResolveBy)99)] class Service;";

        var fixedCode = await ApplyFixAsync(code, "SSC004", "Use ImplementedInterface",
            code.IndexOf('['));

        Assert.Contains("using SharpServiceCollection.Enums;", fixedCode);
        Assert.Contains("ResolveBy.ImplementedInterface", fixedCode);
        Assert.DoesNotContain("global::SharpServiceCollection.Enums", fixedCode);
    }

    [Fact]
    public async Task UnsealedRegistrationDiagnostic_SuggestsSealedClass()
    {
        const string code = "[ServiceRegistrationItem] public class Service;";

        var fixedCode = await ApplyFixAsync(code, "SSC005", "Make the registration class sealed",
            code.IndexOf("class", StringComparison.Ordinal));

        Assert.Contains("public sealed class Service", fixedCode);
    }

    [Fact]
    public async Task MissingInterfaceDiagnostic_SuggestsAddingServiceRegistrationInterface()
    {
        const string code = "[ServiceRegistrationItem] public sealed class Service;";

        var fixedCode = await ApplyFixAsync(
            code,
            "SSC006",
            "Implement IServiceRegistration interface",
            code.IndexOf("class", StringComparison.Ordinal));

        Assert.Contains("using SharpServiceCollection.Interfaces;", fixedCode);
        Assert.Contains("public sealed class Service : IServiceRegistration", fixedCode);
    }

    private static async Task<string> ApplyFixAsync(
        string source,
        string diagnosticId,
        string title,
        int diagnosticStart)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("CodeFixTest", LanguageNames.CSharp);
        var document = workspace.AddDocument(project.Id, "Test.cs", SourceText.From(source));
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                diagnosticId,
                "Test diagnostic",
                "Test diagnostic",
                "Test",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            Location.Create(document.FilePath ?? "Test.cs", new TextSpan(diagnosticStart, 1),
                new LinePositionSpan()));
        var actions = new List<CodeAction>();
        var provider = new SharpServiceCollectionCodeFixProvider();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await provider.RegisterCodeFixesAsync(context);
        var action = Assert.Single(actions, candidate => candidate.Title == title);
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var applyChanges = Assert.Single(operations.OfType<ApplyChangesOperation>());
        var changedDocument = applyChanges.ChangedSolution.GetDocument(document.Id);
        Assert.NotNull(changedDocument);

        return (await changedDocument.GetTextAsync()).ToString();
    }
}