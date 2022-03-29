using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeQualityAnalyzer.CodeMetrics
{
    public static class ResponseForAClass
    {
        public static int GetCount(ClassDeclarationSyntax classNode)
        {
            int methodDeclarations = classNode
                .DescendantNodes()
                .Count(x => x is MethodDeclarationSyntax || x is ConstructorDeclarationSyntax)
                ;

            int methodInvocations = classNode
                .DescendantNodes()
                .Count(x => x is InvocationExpressionSyntax || x is ObjectCreationExpressionSyntax)
                ;
            
            return methodDeclarations + methodInvocations;
        }
    }
}
