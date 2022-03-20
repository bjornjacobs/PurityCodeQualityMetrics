using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity;

public static class FreshAnalyser
{
    public static bool IsReturnFresh(this SyntaxNode node, SemanticModel model)
    {
        IMethodSymbol? method = node switch
        {
            LambdaExpressionSyntax => model.GetSymbolInfo(node).Symbol as IMethodSymbol,
            MethodDeclarationSyntax => model.GetDeclaredSymbol(node) as IMethodSymbol,
            LocalFunctionStatementSyntax => model.GetDeclaredSymbol(node) as IMethodSymbol,
        };

        if (method.ReturnsVoid) return false;
        if (!method.ReturnsByRef) return true;

        var returns = node.DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .Select(x => x.Expression);


        return false;
    }
}