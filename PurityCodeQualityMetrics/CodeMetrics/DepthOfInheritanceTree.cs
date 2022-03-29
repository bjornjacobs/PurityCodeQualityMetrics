using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeQualityAnalyzer.CodeMetrics
{
    public static class DepthOfInheritanceTree
    {
        public static int GetCount(ClassDeclarationSyntax classNode, SemanticModel model)
        {
            int depth = 0;
            var currentClassSymbol = ((ITypeSymbol) model.GetDeclaredSymbol(classNode)).BaseType.BaseType;
            while (currentClassSymbol != null)
            {
                depth++;
                currentClassSymbol = currentClassSymbol.BaseType;
            }

            return depth;
        }
    }
}
