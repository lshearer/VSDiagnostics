﻿using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using VSDiagnostics.Utilities;

namespace VSDiagnostics.Diagnostics.General.AsToCast
{
    [ExportCodeFixProvider(DiagnosticId.AsToCast + "CF", LanguageNames.CSharp), Shared]
    public class AsToCastCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AsToCastAnalyzer.Rule.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var statement = root.FindNode(diagnosticSpan).DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().First();
            context.RegisterCodeFix(
                CodeAction.Create(VSDiagnosticsResources.AsToCastCodeFixTitle,
                    x => AsToCastAsync(context.Document, root, statement), AsToCastAnalyzer.Rule.Id), diagnostic);
        }

        private Task<Document> AsToCastAsync(Document document, SyntaxNode root, SyntaxNode statement)
        {
            var binaryExpression = (BinaryExpressionSyntax) statement;
            var typeSyntax = (TypeSyntax) binaryExpression.Right;

            var newExpression = SyntaxFactory.CastExpression(typeSyntax, binaryExpression.Left).WithAdditionalAnnotations(Formatter.Annotation);
            root = root.ReplaceNode(binaryExpression, newExpression);
            return Task.FromResult(document.WithSyntaxRoot(root));
        }
    }
}