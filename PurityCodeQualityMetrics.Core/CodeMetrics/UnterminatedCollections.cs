using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.CodeMetrics
{
    public static class UnterminatedCollections
    {
        public static int GetCount(SyntaxNode node, SemanticModel semanticModel)
        {
            return node
                .DescendantNodesInThisFunction()
                .OfType<VariableDeclarationSyntax>()
                .Select(x => x.ChildNodes().First())
                .Select(x => semanticModel.GetSymbolInfo(x))
                .Count(x => x.Symbol?.Name == "IEnumerable")
            ;
            //IEnumerable bla = Enumerable.to;
        }
    }
}
