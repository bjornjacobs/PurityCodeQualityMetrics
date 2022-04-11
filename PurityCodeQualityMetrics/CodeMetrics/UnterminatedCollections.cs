using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.CodeMetrics
{
    public static class UnterminatedCollections
    {
        public static int GetCount(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
        {
            return classDeclaration
                .DescendantNodes()
                .OfType<VariableDeclarationSyntax>()
                .Select(x => x.ChildNodes().First())
                .Select(x => semanticModel.GetSymbolInfo(x))
                .Count(x => x.Symbol?.Name == "IEnumerable")
            ;
            //IEnumerable bla = Enumerable.to;
        }
    }
}
