using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.CodeMetrics
{
    public static class DepthOfInheritanceTree
    {
        public static int GetCount(ClassDeclarationSyntax classNode, SemanticModel model)
        {
            if (classNode == null) return -1;
            int depth = 0;
            var symbol = model.GetDeclaredSymbol(classNode) as ITypeSymbol;
            if (symbol == null)
                return -1;
            
            var currentClassSymbol = symbol.BaseType.BaseType;
            while (currentClassSymbol != null)
            {
                depth++;
                currentClassSymbol = currentClassSymbol.BaseType;
            }

            return depth;
        }
    }
}
