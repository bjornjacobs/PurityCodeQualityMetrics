using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Util;

public static class SyntaxNodeUtil
{
    public static bool IsAssignedTo(this SyntaxNode node)
    {
        return !node.IsNotAssignedTo();
    }
    
    public static bool IsNotAssignedTo(this SyntaxNode node)
    {
        return node.Parent == null ||
               node.Parent is AssignmentExpressionSyntax assignmentSyntax && assignmentSyntax.Right.Equals(node) ||
               node.Parent is not AssignmentExpressionSyntax && node.Parent.IsNotAssignedTo();
    }

    public static SyntaxNode? GetMethodThatBelongsTo(this SyntaxNode node)
    {
        if (node.Parent == null) return null;
        if (node.Parent is MethodDeclarationSyntax or LocalFunctionStatementSyntax) return node.Parent;
        return GetMethodThatBelongsTo(node.Parent);
    }
    
    public static IMethodSymbol? GetMethodSymbol(this SyntaxNode node, SemanticModel model)
    {
        return node switch
        {
            LambdaExpressionSyntax => (IMethodSymbol) model.GetSymbolInfo(node).Symbol!,
            LocalFunctionStatementSyntax =>(IMethodSymbol) model.GetDeclaredSymbol(node)!,
            MethodDeclarationSyntax => (IMethodSymbol) model.GetDeclaredSymbol(node)!
        };
    }
    
    public static int GetLambdaCount(this SyntaxNode node)
    {

        var method = node.GetMethodThatBelongsTo();
        if (method == null) 
            return node.SyntaxTree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToList().FindIndex(node.IsEquivalentTo) + 1000; //Think of a better solution
        
        return method.DescendantNodes().OfType<LambdaExpressionSyntax>().ToList().FindIndex(node.IsEquivalentTo);
    }
}