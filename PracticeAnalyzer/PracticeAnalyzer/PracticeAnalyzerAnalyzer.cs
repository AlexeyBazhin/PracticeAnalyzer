using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace PracticeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PracticeAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PracticeAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        
        //private static readonly LocalizableString TitleAutoGen = new LocalizableResourceString(nameof(Resources.AutoGenTitle), Resources.ResourceManager, typeof(Resources));
        //private static readonly LocalizableString MessageFormatAutoGen = new LocalizableResourceString(nameof(Resources.AutoGenMessage), Resources.ResourceManager, typeof(Resources));
        //private static readonly LocalizableString DescriptionAutoGen = new LocalizableResourceString(nameof(Resources.AutoGenDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString TitleNotStored = new LocalizableResourceString(nameof(Resources.NotStoredAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString TitlePropertyStorage = new LocalizableResourceString(nameof(Resources.PropertyStorageAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString TitleValueType = new LocalizableResourceString(nameof(Resources.ValueTypeAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        //private static readonly DiagnosticDescriptor RuleAutoGen = new DiagnosticDescriptor(DiagnosticId, TitleAutoGen, MessageFormatAutoGen, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: DescriptionAutoGen);

        private static readonly DiagnosticDescriptor RuleNotStored = new DiagnosticDescriptor(DiagnosticId, TitleNotStored, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor RulePropertyStorage = new DiagnosticDescriptor(DiagnosticId, TitlePropertyStorage, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor RuleValueType = new DiagnosticDescriptor(DiagnosticId, TitleValueType, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);


        //public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleNotStored, RulePropertyStorage, RuleValueType, RuleAutoGen); } }
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleNotStored, RulePropertyStorage, RuleValueType); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information            

            //context.RegisterSyntaxNodeAction(AnalyzeNameSpace, SyntaxKind.NamespaceDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.PropertyDeclaration);
        }

        //private static void AnalyzeNameSpace(SyntaxNodeAnalysisContext context)
        //{
        //    var namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;

        //    var namespaceKeywordToken = namespaceDeclaration.ChildTokens().Where(token => token.IsKind(SyntaxKind.NamespaceKeyword)).First();

        //    if (namespaceKeywordToken.HasLeadingTrivia)
        //    {
        //        context.ReportDiagnostic(Diagnostic.Create(RuleAutoGen, namespaceKeywordToken.GetLocation()));
        //    }
        //}
        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
            
            if (!IsDataObject(propertyDeclaration, context))
            {
                return;
            }

            NotStoredWithoutDSE(context, propertyDeclaration);
            PropertyStorageNotSpecified(context, propertyDeclaration);
            ValueTypeNotNull(context, propertyDeclaration);            
        }
        private static bool IsDataObject(PropertyDeclarationSyntax propertyDeclaration, SyntaxNodeAnalysisContext context)
        {

            INamedTypeSymbol iSymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration.Parent) as INamedTypeSymbol;
            INamedTypeSymbol symbolBaseType = iSymbol?.BaseType;

            while (symbolBaseType != null)
            {
                if (symbolBaseType.ToString() == "ICSSoft.STORMNET.DataObject")
                    return true;
                symbolBaseType = symbolBaseType.BaseType;
            }
            return false;
        }
        private static void NotStoredWithoutDSE(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration)
        {
            var attrList = propertyDeclaration.DescendantNodes().OfType<AttributeListSyntax>();
            if (attrList?.Where(curr => curr.Attributes
                                    .Any(currAttribute => currAttribute.Name.GetText().ToString() == "ICSSoft.STORMNET.NotStored"))
                        ?.Count() != 0
                &&
                attrList?.Where(curr => curr.Attributes
                                    .Any(currAttribute => currAttribute.Name.GetText().ToString() == "ICSSoft.STORMNET.DataServiceExpression"))
                        ?.Count() == 0)
            {                
                context.ReportDiagnostic(Diagnostic.Create(RuleNotStored, context.Node.ChildTokens()
                                                                .Where(token => token.IsKind(SyntaxKind.IdentifierToken)).First()
                                                                .GetLocation(), propertyDeclaration.Identifier.ValueText, "NotStored", "ICSSoft.STORMNET.DataServiceExpression"));
            }
        }
        private static void PropertyStorageNotSpecified(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration)
        {
            var attrList = propertyDeclaration.DescendantNodes().OfType<AttributeListSyntax>();
            if (attrList?.Where(curr => curr.Attributes
                                    .Any(currAttribute => currAttribute.Name.GetText().ToString() == "ICSSoft.STORMNET.NotStored"))
                ?.Count() == 0
                &&
                attrList?.Where(curr => curr.Attributes
                                    .Any(currAttribute => currAttribute.Name.GetText().ToString() == "ICSSoft.STORMNET.PropertyStorage"))
                ?.Count() == 0
            )
            {
                context.ReportDiagnostic(Diagnostic.Create(RulePropertyStorage, context.Node.ChildTokens()
                                                                .Where(token => token.IsKind(SyntaxKind.IdentifierToken)).First()
                                                                .GetLocation(), propertyDeclaration.Identifier.ValueText, "PropertyStorage", "ICSSoft.STORMNET.PropertyStorage"));
            }
        }
        private static void ValueTypeNotNull(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration)
        {
            ITypeSymbol iSymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration).Type;
                     
            if (iSymbol.IsValueType &&
                propertyDeclaration.DescendantNodes().OfType<AttributeListSyntax>()
                    ?.Where(curr => curr.Attributes
                                    .Any(currAttribute => currAttribute.Name.GetText().ToString() == "ICSSoft.STORMNET.NotNull"))
                    ?.Count() == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleValueType, context.Node.ChildTokens()
                                                                .Where(token => token.IsKind(SyntaxKind.IdentifierToken)).First()
                                                                .GetLocation(), propertyDeclaration.Identifier.ValueText, "ValueType", "ICSSoft.STORMNET.NotNull"));
            }
        }
    }
}
