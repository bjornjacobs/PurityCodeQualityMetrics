using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity;

public static class MethodRetrievers
{
    public static List<CSharpSyntaxNode> GetAllMethods(this SyntaxTree tree)
    {
        var methods = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Cast<CSharpSyntaxNode>();

        var local = tree.GetRoot().DescendantNodes()
            .OfType<LocalFunctionStatementSyntax>()
            .Cast<CSharpSyntaxNode>();
        
        var lamda = tree.GetRoot().DescendantNodes()
            .OfType<LambdaExpressionSyntax>()
            .Cast<CSharpSyntaxNode>();

        return local.Concat(methods).Concat(lamda).ToList();
    }
}