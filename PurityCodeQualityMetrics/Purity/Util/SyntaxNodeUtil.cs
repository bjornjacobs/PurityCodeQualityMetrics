﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Util;

public static class SyntaxNodeUtil
{
    public static bool IsAssignedTo(this SyntaxNode node)
    {
        return !node.IsNotAssignedTo();
    }
    
    public static bool IsAssignedToField(this SyntaxNode node)
    {
        var v = !node.IsNotAssignedTo() && node.Parent.ChildTokens().Any(x => x.IsKind(SyntaxKind.DotToken));
        return v;
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
            MethodDeclarationSyntax => (IMethodSymbol) model.GetDeclaredSymbol(node)!,
            AccessorDeclarationSyntax => (IMethodSymbol) model.GetDeclaredSymbol(node)!,
            _ => throw new NotImplementedException()
        };
    }
    
    public static int GetLambdaCount(this SyntaxNode node)
    {

        var method = node.GetMethodThatBelongsTo();
        if (method == null) 
            return node.SyntaxTree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToList().FindIndex(node.IsEquivalentTo) + 1000; //Think of a better solution
        
        return method.DescendantNodes().OfType<LambdaExpressionSyntax>().ToList().FindIndex(node.IsEquivalentTo);
    }
    
    public static List<CSharpSyntaxNode> GetAllMethods(this SyntaxTree tree)
    {
        var methods = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Cast<CSharpSyntaxNode>();

        var local = tree.GetRoot().DescendantNodes()
            .OfType<LocalFunctionStatementSyntax>()
            .Cast<CSharpSyntaxNode>();
        
        var lambda = tree.GetRoot().DescendantNodes()
            .OfType<LambdaExpressionSyntax>()
            .Cast<CSharpSyntaxNode>();

        var properties = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>();

        return local.Concat(methods).Concat(lambda).Concat(properties).ToList();
    }

    /// <summary>
    /// Extension on DescendantNodes() that excludes nodes that are part of a lambda or local method that are defined in the given method/ functioin
    /// </summary>
    /// <param name="function">A method/ lambda or local function</param>
    /// <returns>All nodes that are defined in the given function</returns>
    public static IEnumerable<SyntaxNode> DescendantNodesInThisFunction(this SyntaxNode function)
    {
        if (function is MethodDeclarationSyntax m)
        {
            function = m.Body;
        }
        return function.DescendantNodes().Where(x => x.IsInFunction(function));
    }

    private static bool IsInFunction(this SyntaxNode node, SyntaxNode function)
    {
        if (node.Parent == null) return false;
        if (node.Parent.Equals(function)) return true;
        if (node.Parent is MethodDeclarationSyntax or LocalFunctionStatementSyntax or LambdaExpressionSyntax)
            return false;
        
        return IsInFunction(node.Parent, function);
    }

    public static bool IsTopLevel(this SyntaxNode node)
    {
        var isThis = node.Parent.ChildNodes().Any(x => x is ThisExpressionSyntax);
        return isThis ||  node.Parent.ChildNodesAndTokens()
            .All(x => !x.IsKind(SyntaxKind.DotToken) || x.GetPreviousSibling() == node );
    }
}