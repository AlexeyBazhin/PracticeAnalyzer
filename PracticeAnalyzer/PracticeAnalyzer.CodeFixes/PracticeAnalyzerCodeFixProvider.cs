using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PracticeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PracticeAnalyzerCodeFixProvider)), Shared]
    public class PracticeAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(PracticeAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {             
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();           
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            //string rule = diagnostic.Descriptor.Description.ToString();
            string rule = diagnostic.Descriptor.Title.ToString();

            //if (rule == "AutoGen")
            //{
            //    // Find the type declaration identified by the diagnostic.
            //    var namespaceDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().First();
            //    // Register a code action that will invoke the fix.
            //    context.RegisterCodeFix(
            //        CodeAction.Create(
            //            title: "DeleteTrivia",
            //            createChangedDocument: c => DeleteAutoGenTriviaAsync(context.Document, namespaceDeclaration, c),
            //            equivalenceKey: "DeleteTrivia"),
            //        diagnostic);
            //    return;
            //}

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();
            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle + "ICSSoft.STORMNET." + rule,
                    createChangedDocument: c => AddAttributeAsync(context.Document, declaration, c, rule),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        //private static async Task<Document> DeleteAutoGenTriviaAsync(Document document, NamespaceDeclarationSyntax namespaceDeclaration,
        //                                                   CancellationToken cancellationToken)
        //{
        //    var namespaceKeywordToken = namespaceDeclaration.ChildTokens().Where(token => token.IsKind(SyntaxKind.NamespaceKeyword)).First();
            
        //    SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            
        //    SyntaxNode newRoot = oldRoot.ReplaceToken(namespaceKeywordToken, namespaceKeywordToken.WithTrailingTrivia());

        //    return document.WithSyntaxRoot(newRoot);
        //}

        private static async Task<Document> AddAttributeAsync(Document document, PropertyDeclarationSyntax propertyDeclaration,
                                                            CancellationToken cancellationToken, string rule)
        {
            SeparatedSyntaxList<AttributeArgumentSyntax> args = new SeparatedSyntaxList<AttributeArgumentSyntax>();
            IdentifierNameSyntax attrIdentifierName = null;
            switch (rule)
            {
                case "NotStored":
                    {
                        args = SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""))),
                                SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""))),
                            });
                        attrIdentifierName = SyntaxFactory.IdentifierName("DataServiceExpression");
                        break;
                    }
                case "PropertyStorage":
                    {
                        args = SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("")))                                
                            });
                        attrIdentifierName = SyntaxFactory.IdentifierName("PropertyStorage");
                        break;
                    }
                case "ValueType":
                    {
                        attrIdentifierName = SyntaxFactory.IdentifierName("NotNull");
                        break;
                    }
            }
            var attributes = propertyDeclaration.AttributeLists.Add(
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.QualifiedName(
                                    SyntaxFactory.IdentifierName("ICSSoft"), 
                                    SyntaxFactory.IdentifierName("STORMNET")),
                                attrIdentifierName),
                            SyntaxFactory.AttributeArgumentList(args)))));

            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode newRoot = oldRoot.ReplaceNode(propertyDeclaration, propertyDeclaration.WithAttributeLists(attributes));

            return document.WithSyntaxRoot(newRoot);
        }        

    }
}
