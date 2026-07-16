using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpServiceCollection.Constants;
using SharpServiceCollection.Enums;

namespace SharpServiceCollection;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SharpServiceCollectionCodeFixProvider))]
[Shared]
public sealed class SharpServiceCollectionCodeFixProvider : CodeFixProvider
{
    private const string InjectableDependencyName = "InjectableDependency";
    private const string ServiceRegistrationItemName = "ServiceRegistrationItem";
    private const string TryAddPropertyName = "TryAdd";
    private const string InstanceLifetimeTypeName = "InstanceLifetime";
    private const string ResolveByTypeName = "ResolveBy";
    private const string EnumsNamespaceName = "SharpServiceCollection.Enums";
    private const string ServiceRegistrationNamespaceName = "SharpServiceCollection.Interfaces";
    private const string ServiceRegistrationInterfaceName = "IServiceRegistration";

    private const string ServiceRegistrationInterfaceQualifiedName =
        "global::SharpServiceCollection.Interfaces.IServiceRegistration";

    private const string AddServiceRegistrationInterfaceTitle = "Implement IServiceRegistration interface";
    private const string AddServiceRegistrationInterfaceEquivalenceKey = "AddServiceRegistrationInterface";
    private const string SetTryAddTitle = "Set TryAdd to true";
    private const string SetTryAddEquivalenceKey = "SetTryAddTrue";
    private const string RemoveTryAddTitle = "Remove TryAdd";
    private const string RemoveTryAddEquivalenceKey = "RemoveTryAdd";
    private const string ResolveBySelfTitle = "Resolve by implementation type";
    private const string ResolveBySelfEquivalenceKey = "ResolveBySelf";
    private const string MakeSealedTitle = "Make the registration class sealed";
    private const string MakeSealedEquivalenceKey = "MakeServiceRegistrationSealed";
    private const string RemoveAttributeTitle = "Remove ServiceRegistrationItem attribute";
    private const string RemoveAttributeEquivalenceKey = "RemoveServiceRegistrationItem";
    private const string UseTitlePrefix = "Use ";
    private const string UseEquivalenceKeyPrefix = "Use";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        DiagnosticIds.EnumerableRequiresTryAdd,
        DiagnosticIds.MatchingInterfaceMissing,
        DiagnosticIds.InvalidLifetime,
        DiagnosticIds.InvalidResolveBy,
        DiagnosticIds.ServiceRegistrationMustBeSealed,
        DiagnosticIds.ServiceRegistrationMustImplementInterface);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case DiagnosticIds.EnumerableRequiresTryAdd:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            SetTryAddTitle,
                            cancellationToken => SetTryAddAsync(context.Document, diagnostic, cancellationToken),
                            SetTryAddEquivalenceKey),
                        diagnostic);
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            RemoveTryAddTitle,
                            cancellationToken => RemoveTryAddAsync(context.Document, diagnostic, cancellationToken),
                            RemoveTryAddEquivalenceKey),
                        diagnostic);
                    break;

                case DiagnosticIds.MatchingInterfaceMissing:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            ResolveBySelfTitle,
                            cancellationToken => ReplaceAttributeArgumentAsync(
                                context.Document,
                                diagnostic,
                                1,
                                $"{ResolveByTypeName}.{nameof(ResolveBy.Self)}",
                                cancellationToken),
                            ResolveBySelfEquivalenceKey),
                        diagnostic);
                    break;

                case DiagnosticIds.InvalidLifetime:
                    RegisterEnumFixes(
                        context,
                        diagnostic,
                        0,
                        InstanceLifetimeTypeName,
                        nameof(InstanceLifetime.Singleton),
                        nameof(InstanceLifetime.Scoped),
                        nameof(InstanceLifetime.Transient));
                    break;

                case DiagnosticIds.InvalidResolveBy:
                    RegisterEnumFixes(
                        context,
                        diagnostic,
                        1,
                        ResolveByTypeName,
                        nameof(ResolveBy.Self),
                        nameof(ResolveBy.ImplementedInterface),
                        nameof(ResolveBy.MatchingInterface));
                    break;

                case DiagnosticIds.ServiceRegistrationMustBeSealed:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            MakeSealedTitle,
                            cancellationToken => MakeSealedAsync(context.Document, diagnostic, cancellationToken),
                            MakeSealedEquivalenceKey),
                        diagnostic);
                    break;

                case DiagnosticIds.ServiceRegistrationMustImplementInterface:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            AddServiceRegistrationInterfaceTitle,
                            cancellationToken => AddServiceRegistrationInterfaceAsync(
                                context.Document,
                                diagnostic,
                                cancellationToken),
                            AddServiceRegistrationInterfaceEquivalenceKey),
                        diagnostic);
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            RemoveAttributeTitle,
                            cancellationToken => RemoveServiceRegistrationAttributeAsync(
                                context.Document,
                                diagnostic,
                                cancellationToken),
                            RemoveAttributeEquivalenceKey),
                        diagnostic);
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private static void RegisterEnumFixes(
        CodeFixContext context,
        Diagnostic diagnostic,
        int argumentIndex,
        string enumTypeName,
        params string[] values)
    {
        foreach (var value in values)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    $"{UseTitlePrefix}{value}",
                    cancellationToken => ReplaceAttributeArgumentAsync(
                        context.Document,
                        diagnostic,
                        argumentIndex,
                        $"{enumTypeName}.{value}",
                        cancellationToken),
                    $"{UseEquivalenceKeyPrefix}{value}"),
                diagnostic);
        }
    }

    private static async Task<Document> SetTryAddAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var attribute = FindInjectableAttribute(root, diagnostic);
        if (root is null || attribute is null)
        {
            return document;
        }

        var arguments = attribute.ArgumentList?.Arguments ?? default;
        var existing = arguments.FirstOrDefault(argument =>
            argument.NameEquals?.Name.Identifier.ValueText == TryAddPropertyName);
        var replacement = SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))
            .WithNameEquals(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(TryAddPropertyName)));

        var updated = existing is not null
            ? attribute.ReplaceNode(existing, replacement.WithTriviaFrom(existing))
            : attribute.WithArgumentList(
                (attribute.ArgumentList ?? SyntaxFactory.AttributeArgumentList())
                .WithArguments(arguments.Add(replacement)));

        return document.WithSyntaxRoot(root.ReplaceNode(attribute, updated));
    }

    private static async Task<Document> RemoveTryAddAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var attribute = FindInjectableAttribute(root, diagnostic);
        if (root is null || attribute?.ArgumentList is null)
        {
            return document;
        }

        var arguments = attribute.ArgumentList.Arguments;
        var existing = arguments.FirstOrDefault(argument =>
            argument.NameEquals?.Name.Identifier.ValueText == TryAddPropertyName);
        if (existing is null)
        {
            return document;
        }

        var updated = attribute.WithArgumentList(
            attribute.ArgumentList.WithArguments(arguments.Remove(existing)));
        return document.WithSyntaxRoot(root.ReplaceNode(attribute, updated));
    }

    private static async Task<Document> ReplaceAttributeArgumentAsync(
        Document document,
        Diagnostic diagnostic,
        int argumentIndex,
        string replacementText,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var attribute = FindInjectableAttribute(root, diagnostic);
        var arguments = attribute?.ArgumentList?.Arguments;
        if (root is null || attribute is null || arguments is null || arguments.Value.Count <= argumentIndex)
        {
            return document;
        }

        var oldArgument = arguments.Value[argumentIndex];
        var replacement = SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(replacementText))
            .WithTriviaFrom(oldArgument);
        var updatedRoot = root.ReplaceNode(attribute, attribute.ReplaceNode(oldArgument, replacement));
        if (updatedRoot is CompilationUnitSyntax compilationUnit &&
            compilationUnit.Usings.All(usingDirective => usingDirective.Name?.ToString() != EnumsNamespaceName))
        {
            updatedRoot = compilationUnit.AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(EnumsNamespaceName)));
        }

        return document.WithSyntaxRoot(updatedRoot);
    }

    private static async Task<Document> MakeSealedAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var classDeclaration = FindClass(root, diagnostic);
        if (root is null || classDeclaration is null || classDeclaration.Modifiers.Any(modifier =>
                modifier.IsKind(SyntaxKind.AbstractKeyword) || modifier.IsKind(SyntaxKind.SealedKeyword)))
        {
            return document;
        }

        return document.WithSyntaxRoot(root.ReplaceNode(
            classDeclaration,
            classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword))));
    }

    private static async Task<Document> AddServiceRegistrationInterfaceAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var classDeclaration = FindClass(root, diagnostic);
        if (root is null || classDeclaration is null)
        {
            return document;
        }

        var interfaceType = SyntaxFactory.IdentifierName(ServiceRegistrationInterfaceName);
        if (classDeclaration.BaseList?.Types.Any(type =>
                type.Type.ToString() is ServiceRegistrationInterfaceName
                    or ServiceRegistrationInterfaceQualifiedName) == true)
        {
            return document;
        }

        var interfaceBaseType = SyntaxFactory.SimpleBaseType(interfaceType);
        var updated = classDeclaration.BaseList is null
            ? classDeclaration.WithBaseList(SyntaxFactory.BaseList(
                SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(interfaceBaseType)))
            : classDeclaration.WithBaseList(classDeclaration.BaseList.AddTypes(interfaceBaseType));

        var updatedRoot = root.ReplaceNode(classDeclaration, updated);
        if (updatedRoot is CompilationUnitSyntax compilationUnit && compilationUnit.Usings.All(usingDirective =>
                usingDirective.Name?.ToString() != ServiceRegistrationNamespaceName))
        {
            updatedRoot = compilationUnit.AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ServiceRegistrationNamespaceName)));
        }

        return document.WithSyntaxRoot(updatedRoot);
    }

    private static async Task<Document> RemoveServiceRegistrationAttributeAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var classDeclaration = FindClass(root, diagnostic);
        if (root is null || classDeclaration is null)
        {
            return document;
        }

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            var attribute = attributeList.Attributes.FirstOrDefault(IsServiceRegistrationAttribute);
            if (attribute is null)
            {
                continue;
            }

            var remaining = attributeList.Attributes.Remove(attribute);
            if (remaining.Count == 0)
            {
                var updatedClass = classDeclaration.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia);
                return updatedClass is null
                    ? document
                    : document.WithSyntaxRoot(root.ReplaceNode(classDeclaration, updatedClass));
            }

            return document.WithSyntaxRoot(root.ReplaceNode(
                classDeclaration,
                classDeclaration.ReplaceNode(attributeList, attributeList.WithAttributes(remaining))));
        }

        return document;
    }

    private static AttributeSyntax? FindInjectableAttribute(SyntaxNode? root, Diagnostic diagnostic)
    {
        var node = root?.FindNode(diagnostic.Location.SourceSpan);
        if (node is null)
        {
            return null;
        }

        return node.AncestorsAndSelf()
                   .OfType<AttributeSyntax>()
                   .FirstOrDefault(IsInjectableDependencyAttribute)
               ?? node.AncestorsAndSelf()
                   .OfType<AttributeListSyntax>()
                   .SelectMany(attributeList => attributeList.Attributes)
                   .FirstOrDefault(IsInjectableDependencyAttribute);
    }

    private static ClassDeclarationSyntax? FindClass(SyntaxNode? root, Diagnostic diagnostic)
    {
        return root?.FindNode(diagnostic.Location.SourceSpan)
            .AncestorsAndSelf()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();
    }

    private static bool IsInjectableDependencyAttribute(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString();
        return name.EndsWith(InjectableDependencyName, StringComparison.Ordinal) ||
               name.Contains(InjectableDependencyName + "<", StringComparison.Ordinal);
    }

    private static bool IsServiceRegistrationAttribute(AttributeSyntax attribute)
    {
        return attribute.Name.ToString().EndsWith(ServiceRegistrationItemName, StringComparison.Ordinal);
    }
}