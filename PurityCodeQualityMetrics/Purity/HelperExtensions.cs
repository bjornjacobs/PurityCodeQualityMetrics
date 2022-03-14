using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity;

public static class HelperExtensions
{
    public static bool IsEnumConstant(this ISymbol symbol)
    {
        return symbol.ContainingType is {TypeKind: TypeKind.Enum};
    }

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

    public static SyntaxNode GetMethodThatBelongsTo(this SyntaxNode node)
    {
        if (node.Parent == null) return null;
        if (node.Parent is MethodDeclarationSyntax or LocalFunctionStatementSyntax) return node.Parent;
        return GetMethodThatBelongsTo(node.Parent);
    }
}