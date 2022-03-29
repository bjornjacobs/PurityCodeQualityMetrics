using System;
using System.Collections;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeQualityAnalyzer.CodeMetrics
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
