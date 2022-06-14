using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.CodeMetrics
{
    public static class SourceLinesOfLambda
    {
        public static int GetCount(SyntaxNode classNode)
        {
            return classNode
                .DescendantNodes()
                .OfType<LambdaExpressionSyntax>()
                .Select(x => x.GetText().Lines.Count)
                .Sum()
            ;
        }
    }
}
