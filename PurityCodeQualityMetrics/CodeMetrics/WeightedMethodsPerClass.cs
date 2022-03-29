using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeQualityAnalyzer.CodeMetrics
{
    public static class WeightedMethodsPerClass
    {
        public static int GetCount(ClassDeclarationSyntax classNode)
        {
            return classNode
                .ChildNodes()
                .Where(x => x is MethodDeclarationSyntax)
                .Select(CyclomaticComplexity.GetCount)
                .Sum()
                ;
        }
    }
}
